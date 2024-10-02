/// <summary>
/// Takes a CohortDistributionParticipant, does a number of individual transformations, and retuns a transformed Model.Participant
/// </summary>
/// <param name="participant">The CohortDistributionParticipant to be transformed.</param>
/// <returns>The transformed Model.Participant</returns>

namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Model.Enums;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Data.SqlClient;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    public TransformDataService(ICreateResponse createResponse, IExceptionHandler exceptionHandler, ILogger<TransformDataService> logger)
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    [Function("TransformDataService")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        CohortDistributionParticipant cohortDistributionParticipant;
        TransformDataRequestBody requestBody;

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<TransformDataRequestBody>(requestBodyJson);
            cohortDistributionParticipant = requestBody.Participant;
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
        // This function is currently not using the screeningService, but it will do in the future
        // var screeningService = requestBody.ScreeningService;

        try
        {
            // Character transformation
            var transformString = new TransformString();
            cohortDistributionParticipant.NamePrefix = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.NamePrefix);
            cohortDistributionParticipant.FirstName = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.FirstName);
            cohortDistributionParticipant.OtherGivenNames = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.OtherGivenNames);
            cohortDistributionParticipant.FamilyName = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.FamilyName);
            cohortDistributionParticipant.PreviousFamilyName = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.PreviousFamilyName);
            cohortDistributionParticipant.AddressLine1 = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.AddressLine1);
            cohortDistributionParticipant.AddressLine2 = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.AddressLine2);
            cohortDistributionParticipant.AddressLine3 = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.AddressLine3);
            cohortDistributionParticipant.AddressLine4 = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.AddressLine4);
            cohortDistributionParticipant.AddressLine5 = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.AddressLine5);
            cohortDistributionParticipant.Postcode = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.Postcode);
            cohortDistributionParticipant.TelephoneNumber = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.TelephoneNumber);
            cohortDistributionParticipant.MobileNumber = await transformString.CheckParticipantCharactersAsync(cohortDistributionParticipant.MobileNumber);

            // Other transformation rules
            cohortDistributionParticipant = await TransformParticipantAsync(cohortDistributionParticipant);

            // Name prefix transformation
            cohortDistributionParticipant.NamePrefix = await TransformNamePrefixAsync(cohortDistributionParticipant.NamePrefix);

            // address transformation
            if (!string.IsNullOrEmpty(cohortDistributionParticipant.Postcode) &&
                string.IsNullOrEmpty(cohortDistributionParticipant.AddressLine1) &&
                string.IsNullOrEmpty(cohortDistributionParticipant.AddressLine2) &&
                string.IsNullOrEmpty(cohortDistributionParticipant.AddressLine3) &&
                string.IsNullOrEmpty(cohortDistributionParticipant.AddressLine4) &&
                string.IsNullOrEmpty(cohortDistributionParticipant.AddressLine5))
            {
                GetMissingAddress getMissingAddress = new(cohortDistributionParticipant, new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString")));
                cohortDistributionParticipant = getMissingAddress.GetAddress();
            }

            // Converting from CohortDistributionParticipant to Participant and returning
            var response = JsonSerializer.Serialize(cohortDistributionParticipant);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, cohortDistributionParticipant.NhsNumber, "", "", JsonSerializer.Serialize(cohortDistributionParticipant));
            _logger.LogWarning(ex, "exception occurred while running transform data service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private T GetTransformedData<T>(List<RuleResultTree> results, string field, T CurrentValue)
    {
        var result = results.Find(x => x.Rule.RuleName.Split('.')[1] == field);
        if (result == null)
        {
            return CurrentValue;
        }

        return result.ActionResult.Output == null ? CurrentValue : (T)result.ActionResult.Output;
    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant)
    {
        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var re = new RulesEngine.RulesEngine(rules);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
        };

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        participant.FirstName = GetTransformedData<string>(resultList, "FirstName", participant.FirstName);
        participant.FamilyName = GetTransformedData<string>(resultList, "FamilyName", participant.FamilyName);
        participant.NhsNumber = GetTransformedData<string>(resultList, "NhsNumber", participant.NhsNumber);
        participant.NamePrefix = GetTransformedData<string>(resultList, "NamePrefix", participant.NamePrefix);
        participant.Gender = (Gender)GetTransformedData<int>(resultList, "Gender", Convert.ToInt32(participant.Gender));
        participant.OtherGivenNames = GetTransformedData<string>(resultList, "OtherGivenNames", participant.OtherGivenNames);
        participant.PreviousFamilyName = GetTransformedData<string>(resultList, "PreviousFamilyName", participant.PreviousFamilyName);
        participant.AddressLine1 = GetTransformedData<string>(resultList, "AddressLine1", participant.AddressLine1);
        participant.AddressLine2 = GetTransformedData<string>(resultList, "AddressLine2", participant.AddressLine2);
        participant.AddressLine3 = GetTransformedData<string>(resultList, "AddressLine3", participant.AddressLine3);
        participant.AddressLine4 = GetTransformedData<string>(resultList, "AddressLine4", participant.AddressLine4);
        participant.AddressLine5 = GetTransformedData<string>(resultList, "AddressLine5", participant.AddressLine5);
        participant.Postcode = GetTransformedData<string>(resultList, "Postcode", participant.Postcode);
        participant.TelephoneNumber = GetTransformedData<string>(resultList, "TelephoneNumber", participant.TelephoneNumber);
        participant.MobileNumber = GetTransformedData<string>(resultList, "MobileNumber", participant.MobileNumber);
        participant.EmailAddress = GetTransformedData<string>(resultList, "EmailAddress", participant.EmailAddress);

        return participant;
    }

    public async Task<string> TransformNamePrefixAsync(string namePrefix)
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
}
