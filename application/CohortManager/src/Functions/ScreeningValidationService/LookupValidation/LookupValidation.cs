namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;
using Data.Database;
using System.Data.SqlClient;

public class LookupValidation
{
    private readonly IExceptionHandler _handleException;

    private readonly ICreateResponse _createResponse;

    private readonly ILogger<LookupValidation> _logger;

    private readonly IReadRulesFromBlobStorage _readRulesFromBlobStorage;

    public LookupValidation(ICreateResponse createResponse, IExceptionHandler handleException, ILogger<LookupValidation> logger, IReadRulesFromBlobStorage readRulesFromBlobStorage)
    {
        _createResponse = createResponse;
        _handleException = handleException;
        _logger = logger;
        _readRulesFromBlobStorage = readRulesFromBlobStorage;
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
            // Set up rules engine
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
            };

            // Execute rules
            var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

            var validationErrors = resultList.Where(x => x.IsSuccess == false);

            if (validationErrors.Any())
            {
                var participantCsvRecord = new ParticipantCsvRecord()
                {
                    Participant = newParticipant,
                    FileName = requestBody.FileName
                };
                var exceptionCreated = await _handleException.CreateValidationExceptionLog(validationErrors, participantCsvRecord);
                if (exceptionCreated)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
                }
            }

            // GP practice check
            DbLookupValidationBreastScreening dbLookup = new(new SqlConnection(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString")));
            if (!dbLookup.ValidatePrimaryCareProvider(newParticipant.PrimaryCareProvider))
            {
                await _handleException.CreateSystemExceptionLog(new ArgumentException("Invalid Primary Care Provider"), newParticipant);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
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
                return "CohortRules.json";
            case RulesType.ParticipantManagement:
                return "lookupRules.json";
            default:
                return "lookupRules.json";
        }
    }
}
