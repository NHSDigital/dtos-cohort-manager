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
using Microsoft.Data.SqlClient;
using RulesEngine.Actions;
using System.ComponentModel.DataAnnotations;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBsTransformationLookups _transformationLookups;
    private readonly ITransformDataLookupFacade _dataLookup;
    public TransformDataService(
        ICreateResponse createResponse,
        IExceptionHandler exceptionHandler,
        ILogger<TransformDataService> logger,
        IBsTransformationLookups transformationLookups,
        ITransformDataLookupFacade dataLookup
    )
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _transformationLookups = transformationLookups;
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
                requestBodyJson = reader.ReadToEnd();
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
            // Character transformation
            var transformString = new TransformString();
            participant = await transformString.TransformStringFields(participant);

            // Database lookup transformations
            participant = await LookupTransformations(participant);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant);

            // Name prefix transformation
            participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix);

            participant = await ReasonForRemovalTransformations(participant);

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

    /// <summary>
    /// Provides transformations to ensure a dummy GP Practice code is given to RfR participants when required.
    /// This logic involves 4 rules which are triggered in order.
    /// If any of the rules are triggered, the subsequent ones are not triggered and the transformation ends.
    /// </summary>
    /// <param name="participant">The participant</param>
    /// <returns>Either a number of transformations if rules 1 or 2 are triggered, or raises an exception if rules 3 or 4 are triggered</returns>
    private async Task<CohortDistributionParticipant> ReasonForRemovalTransformations(CohortDistributionParticipant participant)
    {
        var participantNotRegisteredToGP = new string[] { "RDR", "RDI", "RPR" }.Contains(participant.ReasonForRemoval);
        var validOutcode = !string.IsNullOrEmpty(participant.Postcode) && _dataLookup.ValidateOutcode(participant.Postcode);
        var existingPrimaryCareProvider = _transformationLookups.GetPrimaryCareProvider(participant.NhsNumber);

        var rule1 = participantNotRegisteredToGP && validOutcode && !string.IsNullOrEmpty(participant.Postcode);
        var rule2 = participantNotRegisteredToGP && !validOutcode && !string.IsNullOrEmpty(existingPrimaryCareProvider) && !existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule3 = participantNotRegisteredToGP && !validOutcode && !string.IsNullOrEmpty(existingPrimaryCareProvider) && existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule4 = participantNotRegisteredToGP && !validOutcode && string.IsNullOrEmpty(existingPrimaryCareProvider);

        if (rule1 || rule2)
        {
            participant.PrimaryCareProviderEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate;
            participant.ReasonForRemovalEffectiveFromDate = null;
            participant.ReasonForRemoval = null;
            participant.PrimaryCareProvider = GetDummyPrimaryCareProvider(participant.Postcode ?? "", existingPrimaryCareProvider, validOutcode);
            return participant;
        }
        else if (rule3)
        {
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "3.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));
            throw new TransformationException("Chained rule 3.ParticipantNotRegisteredToGPWithReasonForRemoval raised an exception");
        }
        else if (rule4)
        {
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "4.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));
            throw new TransformationException("Chained rule 4.ParticipantNotRegisteredToGPWithReasonForRemoval raised an exception");
        }
        else return participant;
    }

    /// <summary>
    /// Creates the dummy primary care provider
    /// If there is a valid postcode, it fetches the BSO code from the BS_SELECT_OUTCODE_MAPPING_LKP table using the outcode
    /// If there is not a valid postcode, it fetches the BSO code from the BS_SELECT_GP_PRACTICE_LKP table using the existing primary care provider
    /// </summary>
    /// <param name="postcode">The participant's postcode</param>
    /// <param name="existingPrimaryCareProvider">The existing primary care provider, which was fetched from the BS_COHORT_DISTRIBUTION table</param>
    /// <param name="validOutcode">Boolean for whether the postcode exists / is valid</param>
    /// <returns>The transformed dummy primary care provider, which is made up of "ZZZ" + BSO code</returns>
    private string GetDummyPrimaryCareProvider(string postcode, string existingPrimaryCareProvider, bool validOutcode)
    {
        var dummyPrimaryCareProvider = "ZZZ";

        if (validOutcode)
        {
            return dummyPrimaryCareProvider + _dataLookup.GetBsoCode(postcode);
        }

        if (!string.IsNullOrEmpty(existingPrimaryCareProvider))
        {
            return dummyPrimaryCareProvider + _transformationLookups.GetBsoCodeUsingPCP(existingPrimaryCareProvider);
        }

        return dummyPrimaryCareProvider;
    }
}
