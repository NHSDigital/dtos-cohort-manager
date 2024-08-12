namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;

public class StaticValidation
{
    private readonly ILogger<StaticValidation> _logger;
    private readonly ICallFunction _callFunction;

    private readonly ICreateResponse _createResponse;

    private readonly IExceptionHandler _handleException;

    private readonly IReadRulesFromBlobStorage _readRulesFromBlobStorage;

    public StaticValidation(ILogger<StaticValidation> logger, ICallFunction callFunction, IExceptionHandler handleException, ICreateResponse createResponse, IReadRulesFromBlobStorage readRulesFromBlobStorage)
    {
        _logger = logger;
        _callFunction = callFunction;
        _handleException = handleException;
        _createResponse = createResponse;
        _readRulesFromBlobStorage = readRulesFromBlobStorage;
    }

    [Function("StaticValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ParticipantCsvRecord participantCsvRecord;

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = reader.ReadToEnd();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBodyJson);
            }

            var json = await _readRulesFromBlobStorage.GetRulesFromBlob(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), Environment.GetEnvironmentVariable("BlobContainerName"), "staticRules.json");
            var rules = JsonSerializer.Deserialize<Workflow[]>(json);

            var reSettings = new ReSettings
            {
                CustomTypes = [typeof(Regex), typeof(RegexOptions), typeof(ValidationHelper), typeof(Status), typeof(Actions)]
            };

            var re = new RulesEngine.RulesEngine(rules, reSettings);

            var ruleParameters = new[] {
                new RuleParameter("participant", participantCsvRecord.Participant),
            };
            var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);
            var validationErrors = resultList.Where(x => x.IsSuccess == false);

            if (validationErrors.Any())
            {
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
            _logger.LogError(ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
