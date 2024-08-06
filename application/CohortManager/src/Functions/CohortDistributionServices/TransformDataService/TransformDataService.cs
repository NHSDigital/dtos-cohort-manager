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

        try
        {
            // Character transformation
            var transformString = new TransformString();

            participant.NamePrefix = await transformString.CheckParticipantCharactersAync(participant.NamePrefix);
            participant.FirstName = await transformString.CheckParticipantCharactersAync(participant.FirstName);
            participant.OtherGivenNames = await transformString.CheckParticipantCharactersAync(participant.OtherGivenNames);
            participant.Surname = await transformString.CheckParticipantCharactersAync(participant.Surname);
            participant.PreviousSurname = await transformString.CheckParticipantCharactersAync(participant.PreviousSurname);
            participant.AddressLine1 = await transformString.CheckParticipantCharactersAync(participant.AddressLine1);
            participant.AddressLine2 = await transformString.CheckParticipantCharactersAync(participant.AddressLine2);
            participant.AddressLine3 = await transformString.CheckParticipantCharactersAync(participant.AddressLine3);
            participant.AddressLine4 = await transformString.CheckParticipantCharactersAync(participant.AddressLine4);
            participant.AddressLine5 = await transformString.CheckParticipantCharactersAync(participant.AddressLine5);
            participant.Postcode = await transformString.CheckParticipantCharactersAync(participant.Postcode);
            participant.TelephoneNumber = await transformString.CheckParticipantCharactersAync(participant.TelephoneNumber);
            participant.MobileNumber = await transformString.CheckParticipantCharactersAync(participant.MobileNumber);

            // Other transformation rules
            participant = await TransformParticipantAsync(participant);

            // Name prefix transformation
            participant.NamePrefix = await TransformNamePrefixAsync(participant.NamePrefix);

            // address transformation
            if (! string.IsNullOrEmpty(participant.Postcode) &&
                string.IsNullOrEmpty(participant.AddressLine1) &&
                string.IsNullOrEmpty(participant.AddressLine2) &&
                string.IsNullOrEmpty(participant.AddressLine3) &&
                string.IsNullOrEmpty(participant.AddressLine4) &&
                string.IsNullOrEmpty(participant.AddressLine5))
            {
                GetAddress(participant, new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString")));
            }

            // Converting from CohortDistributionParticipant to Participant and returning
            var transformedParticipant = new Participant(participant);
            var response = JsonSerializer.Serialize(transformedParticipant);
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
        return result?.ActionResult?.Output == null ? CurrentValue : (T)result.ActionResult.Output;
    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant)
    {
        // This function is currently not using the screeningService, but it will do in the future
        // var screeningService = requestBody.ScreeningService;

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

        return participant;
    }

    public async Task<string> TransformNamePrefixAsync(string namePrefix) {

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
        namePrefix = (string)rulesList.Where(result => result.IsSuccess)
                                                    .Select(result => result.ActionResult.Output)
                                                    .FirstOrDefault()
                                                    ?? namePrefix;

        return namePrefix;
    }

    public CohortDistributionParticipant GetAddress(CohortDistributionParticipant participant, IDbConnection connection) {
        // Set up DB connection
        using (connection)
        {
            connection.Open();

            string sql = $"SELECT POST_CODE, ADDRESS_LINE_1, ADDRESS_LINE_2, ADDRESS_LINE_3, ADDRESS_LINE_4, ADDRESS_LINE_5 " +
                        $"FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                        $"WHERE PARTICIPANT_ID = '{participant.ParticipantId}'";

            using (SqlCommand command = new SqlCommand(sql, (SqlConnection) connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Throw exception if Postcodes don't match
                        if (participant.Postcode != reader.GetString(0)) {
                            // will be changed to call exception service
                            throw new ArgumentException();
                        }

                        // Assign database values to address field
                        participant.AddressLine1 = reader["ADDRESS_LINE_1"] as string ?? null;
                        participant.AddressLine2 = reader["ADDRESS_LINE_2"] as string ?? null;
                        participant.AddressLine3 = reader["ADDRESS_LINE_3"] as string ?? null;
                        participant.AddressLine4 = reader["ADDRESS_LINE_4"] as string ?? null;
                        participant.AddressLine5 = reader["ADDRESS_LINE_5"] as string ?? null;
                    }
                }
            }
        }
        return participant;   
    }

}
