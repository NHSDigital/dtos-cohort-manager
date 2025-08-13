/// <summary>
/// Takes a CohortDistributionParticipant, does a number of individual transformations, and returns a transformed CohortDistributionParticipant
/// </summary>
/// <param name="participant">The CohortDistributionParticipant to be transformed.</param>
/// <returns>The transformed participant</returns>

namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Common;
using Microsoft.Extensions.Logging;
using System.Data;
using RulesEngine.Actions;
using System.Threading.Tasks;
using Hl7.Fhir.Rest;
using Model.Enums;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ITransformReasonForRemoval _transformReasonForRemoval;
    private readonly ITransformDataLookupFacade _dataLookup;

    public TransformDataService(
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        ILogger<TransformDataService> logger,
        ITransformReasonForRemoval transformReasonForRemoval,
        ITransformDataLookupFacade dataLookup
    )
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _transformReasonForRemoval = transformReasonForRemoval;
        _dataLookup = dataLookup;
    }

    [Function("TransformDataService")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        CohortDistributionParticipant participant;
        TransformDataRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<TransformDataRequestBody>(requestBodyJson);
            participant = requestBody.Participant;
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        try
        {
            if (!long.TryParse(participant.NhsNumber, out long nhsNumberLong))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "NHS Number couldn't be parsed to long");
            }

            // Character transformation
            var transformString = new TransformString(_exceptionHandler);
            participant = await transformString.TransformStringFields(participant);

            // Address transformation
            participant = TransformAddress(requestBody.ExistingParticipant, participant);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant, requestBody.ExistingParticipant);

            // Name prefix transformation
            if (participant.NamePrefix != null)
                participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix, participant);


            participant = await _transformReasonForRemoval.ReasonForRemovalTransformations(participant, requestBody.ExistingParticipant);
            if (participant.NhsNumber != null)
            {
                var response = JsonSerializer.Serialize(participant);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req, "");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "An error occurred during transformation");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "", participant.ScreeningName, JsonSerializer.Serialize(participant));
            return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req);
        }
        catch (TransformationException ex)
        {
            _logger.LogWarning(ex, "An error occurred during transformation");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "", participant.ScreeningName, JsonSerializer.Serialize(participant));
            _logger.LogWarning(ex, "exception occurred while running transform data service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant,
                                                                            CohortDistribution databaseParticipant)
    {
        var excludedSMUList = await _dataLookup.GetCachedExcludedSMUValues();

        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var actions = new Dictionary<string, Func<ActionBase>> { { "TransformAction", () => new TransformAction() }, };
        var reSettings = new ReSettings
        {
            CustomActions = actions,
            CustomTypes = [typeof(Actions), typeof(CohortDistributionParticipant), typeof(CohortDistribution)],
            UseFastExpressionCompiler = false
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);
        var existingParticipant = new CohortDistributionParticipant(databaseParticipant); // for Rule which are of NoTransform type like Rule 35
        var ruleParameters = new[] {
            new RuleParameter("databaseParticipant", databaseParticipant),
            new RuleParameter("participant", participant),
            new RuleParameter("dbLookup", _dataLookup),
            new RuleParameter("excludedSMUList", excludedSMUList),
            new RuleParameter("existingParticipant", existingParticipant)
        };

        var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

        var additionalWorkflow = participant.ReferralFlag switch
        {
            null => null,       // Null: no additional rules
            true => "Referred", // True: run Referred rules
            false => "Routine"  // False: run Routine rules
        };

        if (additionalWorkflow != null)
        {
            resultList.AddRange(await re.ExecuteAllRulesAsync(additionalWorkflow, ruleParameters));
        }

        await HandleExceptions(resultList, participant);
        await CreateTransformExecutedExceptions(resultList, participant);

        return participant;
    }
    private async Task<string?> TransformNamePrefixAsync(string namePrefix, CohortDistributionParticipant participant)
    {

        // Set up rules engine
        string json = await File.ReadAllTextAsync("namePrefixRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var reSettings = new ReSettings { UseFastExpressionCompiler = false };
        var re = new RulesEngine.RulesEngine(rules, reSettings);

        namePrefix = namePrefix.ToUpper();

        var ruleParameters = new[] {
            new RuleParameter("namePrefix", namePrefix),
        };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("NamePrefix", ruleParameters);

        bool prefixTransformed = rulesList.Any(r => r.IsSuccess);
        var namePrefixRule = rulesList.Where(result => result.IsSuccess).FirstOrDefault();

        if (namePrefixRule == null)
        {
            await _exceptionHandler.CreateTransformExecutedExceptions(participant, $"Name Prefix Invalid", 83, ExceptionCategory.TransformExecuted);
            return null;
        }
        if (namePrefixRule.Rule.RuleName == "0.NamePrefix.NamePrefixValid")
        {
            return namePrefix;
        }

        var ruleNumber = int.Parse(namePrefixRule.Rule.RuleName.Split('.')[0]);
        var ruleName = namePrefixRule.Rule.RuleName.Split('.')[2];
        namePrefix = (string)namePrefixRule.ActionResult.Output;

        if (prefixTransformed)
        {
            await _exceptionHandler.CreateTransformExecutedExceptions(participant, $"Name Prefix {ruleName}", ruleNumber, ExceptionCategory.TransformExecuted);
            return namePrefix;
        }


        return null;
    }

    /// <summary>
    /// If the request participant's address is missing, this method updates it with the address from
    /// the existing record in the database.
    /// </summary>
    /// <exception cref="ArgumentException">Throws an error if the record's postcodes do not match</exception>
    private static CohortDistributionParticipant TransformAddress(CohortDistribution databaseParticipant,
                                                        CohortDistributionParticipant requestParticipant)
    {
        if (requestParticipant.RecordType != Actions.Amended)
            return requestParticipant;

        if (!string.IsNullOrEmpty(requestParticipant.Postcode) &&
            string.IsNullOrEmpty(requestParticipant.AddressLine1) &&
            string.IsNullOrEmpty(requestParticipant.AddressLine2) &&
            string.IsNullOrEmpty(requestParticipant.AddressLine3) &&
            string.IsNullOrEmpty(requestParticipant.AddressLine4) &&
            string.IsNullOrEmpty(requestParticipant.AddressLine5))
        {
            if (requestParticipant.Postcode != databaseParticipant.PostCode)
                throw new TransformationException("Participant has an empty address and postcode does not match existing record");

            requestParticipant.AddressLine1 = databaseParticipant.AddressLine1;
            requestParticipant.AddressLine2 = databaseParticipant.AddressLine2;
            requestParticipant.AddressLine3 = databaseParticipant.AddressLine3;
            requestParticipant.AddressLine4 = databaseParticipant.AddressLine4;
            requestParticipant.AddressLine5 = databaseParticipant.AddressLine5;
        }

        return requestParticipant;
    }

    private async Task HandleExceptions(List<RuleResultTree> exceptions, CohortDistributionParticipant participant)
    {
        var failedTransforms = exceptions.Where(i => !string.IsNullOrEmpty(i.ExceptionMessage) || (!i.Rule.RuleName.Contains("NoTransformation") && i.IsSuccess && i.ActionResult.Output == null)).ToList();
        if (failedTransforms.Any())
        {
            await _exceptionHandler.CreateTransformationExceptionLog(failedTransforms, participant);
            throw new TransformationException("There was an error during transformation");
        }
    }
    private async Task CreateTransformExecutedExceptions(List<RuleResultTree> exceptions, CohortDistributionParticipant participant)
    {
        var executedTransforms = exceptions.Where(i => i.IsSuccess).ToList();

        foreach (var transform in executedTransforms)
        {
            var ruleDetails = transform.Rule.RuleName.Split('.');

            var ruleId = int.Parse(ruleDetails[0]);
            var ruleName = string.Concat(ruleDetails[1..]);
            await _exceptionHandler.CreateTransformExecutedExceptions(participant, ruleName, ruleId);
        }
    }

}
