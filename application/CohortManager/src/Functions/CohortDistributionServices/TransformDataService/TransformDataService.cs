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
    private readonly IBsTransformationLookups _transformationLookups;
    private readonly ITransformReasonForRemoval _transformReasonForRemoval;
    private readonly IDataServiceClient<CohortDistribution> _CohortDistributionClient;
    public TransformDataService(
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        ILogger<TransformDataService> logger,
        IBsTransformationLookups transformationLookups,
        ITransformReasonForRemoval transformReasonForRemoval,
        IDataServiceClient<CohortDistribution> cohortDistributionClient
    )
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _transformationLookups = transformationLookups;
        _transformReasonForRemoval = transformReasonForRemoval;
        _CohortDistributionClient = cohortDistributionClient;
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

            var existingParticipantsList = await _CohortDistributionClient.GetByFilter(x => x.NHSNumber == nhsNumberLong);

            var lastParticipant = existingParticipantsList.OrderByDescending(x => x.CohortDistributionId).FirstOrDefault();

            // Character transformation
            var transformString = new TransformString();
            participant = await transformString.TransformStringFields(participant);

            // Database lookup transformations
            participant = await LookupTransformations(participant);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant);

            // Name prefix transformation
            if (participant.NamePrefix != null)
                participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix);

            participant = await _transformReasonForRemoval.ReasonForRemovalTransformations(participant,lastParticipant);

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

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant)
    {
        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var actions = new Dictionary<string, Func<ActionBase>> { { "TransformAction", () => new TransformAction() } };
        var reSettings = new ReSettings
        {
            CustomActions = actions,
            CustomTypes = [typeof(Actions)]
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
            new RuleParameter("transformLookups", _transformationLookups)
        };

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        await HandleExceptions(resultList, participant);

        return participant;
    }

    private static async Task<string> TransformNamePrefixAsync(string namePrefix)
    {

        // Set up rules engine
        string json = await File.ReadAllTextAsync("namePrefixRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var re = new RulesEngine.RulesEngine(rules);

        namePrefix = namePrefix.ToUpper();

        var ruleParameters = new[] {
            new RuleParameter("namePrefix", namePrefix),
        };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("NamePrefix", ruleParameters);

        // Assign new name prefix
        namePrefix = (string?)rulesList.Where(result => result.IsSuccess)
                                        .Select(result => result.ActionResult.Output)
                                        .FirstOrDefault()
                                        ?? null;

        bool prefixTransformed = rulesList.Any(r => r.IsSuccess);

        if (!prefixTransformed)
            namePrefix = null;

        return namePrefix;
    }

    /// <summary>
    /// Performs transformations that require database lookups using the BsTransformationLookups class
    /// </summary>
    /// <param name="participant">The CohortDistributionParticipant to be transformed.</param>
    /// <returns>The transformed participant</returns>
    public async Task<CohortDistributionParticipant> LookupTransformations(CohortDistributionParticipant participant)
    {
        // Set up rules engine
        string json = await File.ReadAllTextAsync("lookupTransformationRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var reSettings = new ReSettings { CustomTypes = [typeof(Actions)] };
        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
            new RuleParameter("transformationLookups", _transformationLookups)
        };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("LookupTransformations", ruleParameters);

        await HandleExceptions(rulesList, participant);

        participant.FirstName = GetTransformedData<string>(rulesList, "FirstName", participant.FirstName);
        participant.FamilyName = GetTransformedData<string>(rulesList, "FamilyName", participant.FamilyName);

        // address transformation
        if (!string.IsNullOrEmpty(participant.Postcode) &&
            string.IsNullOrEmpty(participant.AddressLine1) &&
            string.IsNullOrEmpty(participant.AddressLine2) &&
            string.IsNullOrEmpty(participant.AddressLine3) &&
            string.IsNullOrEmpty(participant.AddressLine4) &&
            string.IsNullOrEmpty(participant.AddressLine5))
        {
            participant = _transformationLookups.GetAddress(participant);
        }

        return participant;
    }

    /// <summary>
    /// Gets the result of the transformation from the rule output and assigns it to the relevant field.
    /// Only being used for the rules that require database lookup as the other assignment method does not work.
    /// </summary>
    /// <param name="results">The rule result tree produced from the rule execution</param>
    /// <param name="field">The name of the field</param>
    /// <param name="currentValue">The current value of the field in the participant</param>
    /// <returns>The transformed value, or the current value if null</returns>
    private static T GetTransformedData<T>(List<RuleResultTree> results, string field, T currentValue)
    {
        // The field is the 3rd part of the rule
        var result = results.Find(x => x.Rule.RuleName.Split('.')[2] == field);
        if (result == null) return currentValue;

        return result.ActionResult.Output == null ? currentValue : (T)result.ActionResult.Output;
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
}
