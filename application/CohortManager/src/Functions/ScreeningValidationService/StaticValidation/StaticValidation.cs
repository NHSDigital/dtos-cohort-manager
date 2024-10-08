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

    private readonly ICreateResponse _createResponse;

    private readonly IExceptionHandler _handleException;

    private readonly IReadRulesFromBlobStorage _readRulesFromBlobStorage;

    private readonly ICallFunction _callFunction;

    public StaticValidation(ILogger<StaticValidation> logger, IExceptionHandler handleException, ICreateResponse createResponse, IReadRulesFromBlobStorage readRulesFromBlobStorage, ICallFunction callFunction)
    {
        _logger = logger;
        _handleException = handleException;
        _createResponse = createResponse;
        _readRulesFromBlobStorage = readRulesFromBlobStorage;
        _callFunction = callFunction;
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

            var ruleFileName = $"{participantCsvRecord.Participant.ScreeningName}_staticRules.json".Replace(" ", "_");
            _logger.LogInformation("ruleFileName: {RuleFileName}", ruleFileName);

            var json = await _readRulesFromBlobStorage.GetRulesFromBlob(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                                                                        Environment.GetEnvironmentVariable("BlobContainerName"),
                                                                        ruleFileName);
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

            await RemoveOldValidationRecord(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.Participant.ScreeningName);
            if (validationErrors.Any())
            {
                var createExceptionLogResponse = await _handleException.CreateValidationExceptionLog(validationErrors, participantCsvRecord);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req, JsonSerializer.Serialize(createExceptionLogResponse));
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task RemoveOldValidationRecord(string nhsNumber, string screeningName)
    {
        var OldExceptionRecordJson = JsonSerializer.Serialize(new OldExceptionRecord()
        {
            NhsNumber = nhsNumber,
            ScreeningName = screeningName
        });
        await _callFunction.SendPost(Environment.GetEnvironmentVariable("RemoveOldValidationRecord"), OldExceptionRecordJson);
    }
}
