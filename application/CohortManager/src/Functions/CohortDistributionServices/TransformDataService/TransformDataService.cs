/// <summary>
/// Takes a CohortDistributionParticipant, does a number of individual transformations, and returns a transformed CohortDistributionParticipant
/// </summary>
/// <param name="participant">The CohortDistributionParticipant to be transformed.</param>
/// <returns>The transformed participant</returns>

namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using System.Data;
using RulesEngine.Actions;
using DataServices.Client;
using System.Configuration;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ITransformReasonForRemoval _transformReasonForRemoval;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    public TransformDataService(
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        ILogger<TransformDataService> logger,
        ITransformReasonForRemoval transformReasonForRemoval,
        IDataServiceClient<CohortDistribution> cohortDistributionClient
    )
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _transformReasonForRemoval = transformReasonForRemoval;
        _cohortDistributionClient = cohortDistributionClient;
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

            var existingParticipantsList = await _cohortDistributionClient.GetByFilter(x => x.NHSNumber == nhsNumberLong);
            var lastParticipant = existingParticipantsList.OrderByDescending(x => x.CohortDistributionId).FirstOrDefault();

            // Character transformation
            var transformString = new TransformString(_exceptionHandler);
            participant = await transformString.TransformStringFields(participant);

            // Address transformation
            participant = TransformAddress(lastParticipant, participant);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant, lastParticipant);

            // Name prefix transformation
            if (participant.NamePrefix != null)
                participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix,participant);


            participant = await _transformReasonForRemoval.ReasonForRemovalTransformations(participant, lastParticipant);

            var response = JsonSerializer.Serialize(participant);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
        }
        catch (TransformationException ex)
        {
            _logger.LogWarning(ex, "An error occurred during transformation");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "", "", JsonSerializer.Serialize(participant));
            _logger.LogWarning(ex, "exception occurred while running transform data service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant,
                                                                            CohortDistribution databaseParticipant)
    {
        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var actions = new Dictionary<string, Func<ActionBase>> { { "TransformAction", () => new TransformAction() } };
        var reSettings = new ReSettings
        {
            CustomActions = actions,
            CustomTypes = [typeof(Actions)],
            UseFastExpressionCompiler = false
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("databaseParticipant", databaseParticipant),
            new RuleParameter("participant", participant),
        };

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        await HandleExceptions(resultList, participant);
        await CreateTransformExecutedExceptions(resultList,participant);

        return participant;
    }

    private async Task<string> TransformNamePrefixAsync(string namePrefix, CohortDistributionParticipant participant)
    {

        // Set up rules engine
        string json = await File.ReadAllTextAsync("namePrefixRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var reSettings = new ReSettings {UseFastExpressionCompiler = false};
        var re = new RulesEngine.RulesEngine(rules, reSettings);

        namePrefix = namePrefix.ToUpper();

        var ruleParameters = new[] {
            new RuleParameter("namePrefix", namePrefix),
        };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("NamePrefix", ruleParameters);

        // // Assign new name prefix
        // namePrefix = (string?)rulesList.Where(result => result.IsSuccess)
        //                                 .Select(result => result.ActionResult.Output)
        //                                 .FirstOrDefault()
        //                                 ?? null;


        bool prefixTransformed = rulesList.Any(r => r.IsSuccess);



        var namePrefixRule = rulesList.Where(result => result.IsSuccess).FirstOrDefault();
        if(namePrefixRule == null)
        {
            _exceptionHandler.CreateTransformExecutedExceptions(participant,$"Name Prefix Invalid",83);
            return null;
        }
        if(namePrefixRule.Rule.RuleName == "0.NamePrefix.NamePrefixValid")
        {
            return namePrefix;
        }

        var ruleNumber = int.Parse(namePrefixRule.Rule.RuleName.Split('.')[0]);
        var ruleName = namePrefixRule.Rule.RuleName.Split('.')[2];
        namePrefix = (string)namePrefixRule.ActionResult.Output;

        if (prefixTransformed)
        {
            _exceptionHandler.CreateTransformExecutedExceptions(participant,$"Name Prefix {ruleName}",ruleNumber);
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
                throw new ArgumentException("Participant has an empty address and postcode does not match existing record");

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
        var failedTransforms = exceptions.Where(i => !string.IsNullOrEmpty(i.ExceptionMessage) ||
                                                i.IsSuccess && i.ActionResult.Output == null).ToList();
        if (failedTransforms.Any())
        {
            await _exceptionHandler.CreateTransformationExceptionLog(failedTransforms, participant);
            throw new TransformationException("There was an error during transformation");
        }
    }
    private async Task CreateTransformExecutedExceptions(List<RuleResultTree> exceptions, CohortDistributionParticipant participant){
        var executedTransforms = exceptions.Where(i => i.IsSuccess).ToList();

        foreach(var transform in executedTransforms)
        {
            var ruleDetails = transform.Rule.RuleName.Split('.');

            var ruleId = int.Parse(ruleDetails[0]);
            var ruleName = string.Concat(ruleDetails[1..]);
            await _exceptionHandler.CreateTransformExecutedExceptions(participant,ruleName,ruleId);
        }
    }

}
