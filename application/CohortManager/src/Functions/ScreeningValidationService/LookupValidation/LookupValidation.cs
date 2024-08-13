namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;

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
        LookupValidationRequestBody requestBody;

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = reader.ReadToEnd();
                requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);
            }
        }
        catch
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        Participant newParticipant = null;

        try
        {
            var existingParticipant = requestBody.ExistingParticipant;
            newParticipant = requestBody.NewParticipant;

            var json = await _readRulesFromBlobStorage.GetRulesFromBlob(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), Environment.GetEnvironmentVariable("BlobContainerName"), "lookupRules.json");
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
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _handleException.CreateSystemExceptionLog(ex, newParticipant, "");
            _logger.LogWarning(ex, $"Error while processing lookup Validation message: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);

        }
    }
}
