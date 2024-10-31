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

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDbLookupValidationBreastScreening _dbLookup;
    public TransformDataService(ICreateResponse createResponse, IExceptionHandler exceptionHandler, ILogger<TransformDataService> logger, IDbLookupValidationBreastScreening dbLookup)
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _dbLookup = dbLookup;
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
        var actions = new Dictionary<string, Func<ActionBase>> { { "TransformAction", () => new TransformAction() }, { "TransformError", () => new TransformError() } };
        var reSettings = new ReSettings { CustomActions = actions };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
            new RuleParameter("dbLookup", _dbLookup),
            new RuleParameter("bsoCode", _dbLookup.RetrieveBSOCode(participant.Postcode))
        };

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        var result = resultList.Where(result => result.IsSuccess)
            .Select(result => result.ActionResult.Output)
            .FirstOrDefault();

        if (result is Exception exception)
        {
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = new Participant(participant),
                FileName = "",
            };

            try
            {
                _logger.LogInformation("A transformation rule raised an exception: {ExceptionMessage}", exception.Message);
                await _exceptionHandler.CreateValidationExceptionLog(resultList.Where(result => result.IsSuccess), participantCsvRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handling the exception failed. Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
                await _exceptionHandler.CreateSystemExceptionLog(ex, participantCsvRecord.Participant, participantCsvRecord.FileName);
            }

            return participant;
        }

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
}
