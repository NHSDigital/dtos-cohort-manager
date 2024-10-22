namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;

public class LookupValidation
{
    private readonly IExceptionHandler _handleException;
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<LookupValidation> _logger;
    private readonly IReadRulesFromBlobStorage _readRulesFromBlobStorage;
    private IDbLookupValidationBreastScreening _dbLookup;

    public LookupValidation(ICreateResponse createResponse, IExceptionHandler handleException, ILogger<LookupValidation> logger,
                            IReadRulesFromBlobStorage readRulesFromBlobStorage, IDbLookupValidationBreastScreening dbLookup)
    {
        _createResponse = createResponse;
        _handleException = handleException;
        _logger = logger;
        _readRulesFromBlobStorage = readRulesFromBlobStorage;
        _dbLookup = dbLookup;
    }

    [Function("LookupValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        LookupValidationRequestBody requestBody = new LookupValidationRequestBody();

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = reader.ReadToEnd();
                requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            await _handleException.CreateSystemExceptionLog(ex, requestBody.ExistingParticipant, requestBody.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        Participant newParticipant = null;

        try
        {
            var existingParticipant = requestBody.ExistingParticipant;
            newParticipant = requestBody.NewParticipant;

            var ruleFileName = $"{newParticipant.ScreeningName}_{GetValidationRulesName(requestBody.RulesType)}".Replace(" ", "_");
            _logger.LogInformation("ruleFileName {ruleFileName}", ruleFileName);

            var json = await _readRulesFromBlobStorage.GetRulesFromBlob(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                                                                        Environment.GetEnvironmentVariable("BlobContainerName"),
                                                                        ruleFileName);
            var rules = JsonSerializer.Deserialize<Workflow[]>(json);

            var reSettings = new ReSettings
            {
                CustomTypes = [typeof(Actions)]
            };
            var re = new RulesEngine.RulesEngine(rules, reSettings);
            var ruleParameters = new[] {
                new RuleParameter("existingParticipant", existingParticipant),
                new RuleParameter("newParticipant", newParticipant),
                new RuleParameter("dbLookup", _dbLookup)
            };

            var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

            if (re.GetAllRegisteredWorkflowNames().Contains(newParticipant.RecordType))
            {
                var ActionResults = await re.ExecuteAllRulesAsync(newParticipant.RecordType, ruleParameters);
                resultList.AddRange(ActionResults);
            }

            // Validation rules are logically reversed
            var validationErrors = resultList.Where(x => !x.IsSuccess);

            if (validationErrors.Any())
            {
                var participantCsvRecord = new ParticipantCsvRecord()
                {
                    Participant = newParticipant,
                    FileName = requestBody.FileName
                };
                var exceptionCreated = await _handleException.CreateValidationExceptionLog(validationErrors, participantCsvRecord);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req, JsonSerializer.Serialize(exceptionCreated));

            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            }));
        }
        catch (Exception ex)
        {
            _handleException.CreateSystemExceptionLog(ex, newParticipant, "");
            _logger.LogError(ex, $"Error while processing lookup Validation message: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);

        }
    }

    private string GetValidationRulesName(RulesType rulesType)
    {
        switch (rulesType)
        {
            case RulesType.CohortDistribution:
                return "cohortRules.json";
            case RulesType.ParticipantManagement:
                return "lookupRules.json";
            default:
                return "lookupRules.json";
        }
    }
}
