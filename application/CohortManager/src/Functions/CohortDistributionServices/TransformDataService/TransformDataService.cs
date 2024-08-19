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
using System.Data.SqlClient;

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
        // This function is currently not using the screeningService, but it will do in the future
        // var screeningService = requestBody.ScreeningService;

        try
        {
            // Character transformation
            var transformString = new TransformString();
            participant.NamePrefix = await transformString.CheckParticipantCharactersAsync(participant.NamePrefix);
            participant.FirstName = await transformString.CheckParticipantCharactersAsync(participant.FirstName);
            participant.OtherGivenNames = await transformString.CheckParticipantCharactersAsync(participant.OtherGivenNames);
            participant.Surname = await transformString.CheckParticipantCharactersAsync(participant.Surname);
            participant.PreviousSurname = await transformString.CheckParticipantCharactersAsync(participant.PreviousSurname);
            participant.AddressLine1 = await transformString.CheckParticipantCharactersAsync(participant.AddressLine1);
            participant.AddressLine2 = await transformString.CheckParticipantCharactersAsync(participant.AddressLine2);
            participant.AddressLine3 = await transformString.CheckParticipantCharactersAsync(participant.AddressLine3);
            participant.AddressLine4 = await transformString.CheckParticipantCharactersAsync(participant.AddressLine4);
            participant.AddressLine5 = await transformString.CheckParticipantCharactersAsync(participant.AddressLine5);
            participant.Postcode = await transformString.CheckParticipantCharactersAsync(participant.Postcode);
            participant.TelephoneNumber = await transformString.CheckParticipantCharactersAsync(participant.TelephoneNumber);
            participant.MobileNumber = await transformString.CheckParticipantCharactersAsync(participant.MobileNumber);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant);

            // Name prefix transformation
            participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix);

            // address transformation
            if (!string.IsNullOrEmpty(participant.Postcode) &&
                string.IsNullOrEmpty(participant.AddressLine1) &&
                string.IsNullOrEmpty(participant.AddressLine2) &&
                string.IsNullOrEmpty(participant.AddressLine3) &&
                string.IsNullOrEmpty(participant.AddressLine4) &&
                string.IsNullOrEmpty(participant.AddressLine5))
            {
                GetMissingAddress getMissingAddress = new(participant, new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString")));
                participant = getMissingAddress.GetAddress();
            }

            var response = JsonSerializer.Serialize(participant);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "");
            _logger.LogWarning(ex, "exception occured while running transform data service");
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
        participant.Surname = GetTransformedData<string>(resultList, "Surname", participant.Surname);
        participant.NhsNumber = GetTransformedData<string>(resultList, "NhsNumber", participant.NhsNumber);
        participant.NamePrefix = GetTransformedData<string>(resultList, "NamePrefix", participant.NamePrefix);
        participant.Gender = (Gender)GetTransformedData<int>(resultList, "Gender", Convert.ToInt32(participant.Gender));
        participant.OtherGivenNames = GetTransformedData<string>(resultList, "OtherGivenNames", participant.OtherGivenNames);
        participant.PreviousSurname = GetTransformedData<string>(resultList, "PreviousSurname", participant.PreviousSurname);
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
                                                    ?? namePrefix;

        return namePrefix;
    }
}
