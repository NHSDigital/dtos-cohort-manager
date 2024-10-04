/// <summary>
/// Takes a CohortDistributionParticipant, does a number of individual transformations, and retuns a transformed CohortDistributionParticipant
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
            participant = await transformString.TransformStringFields(participant);

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
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "", "", JsonSerializer.Serialize(participant));
            _logger.LogWarning(ex, "exception occurred while running transform data service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(CohortDistributionParticipant participant)
    {
        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var action = new Dictionary<string, Func<ActionBase>>{{"TransformAction", () => new TransformAction()}};
        var reSettings = new ReSettings {CustomActions = action};

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
        };

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        var transformedParticipant = (CohortDistributionParticipant)resultList.Where(result => result.IsSuccess)
                                                    .Select(result => result.ActionResult.Output)
                                                    .FirstOrDefault()
                                                    ?? participant;

        return transformedParticipant;
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
