namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using NHS.Screening.StaticValidation;
using RulesEngine.Models;

public class StaticValidation
{
    private readonly ILogger<StaticValidation> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly IReadRules _readRules;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly StaticValidationConfig _config;

    public StaticValidation(
        ILogger<StaticValidation> logger,
        IExceptionHandler handleException,
        ICreateResponse createResponse,
        IReadRules readRules,
        IHttpClientFunction httpClientFunction,
        IOptions<StaticValidationConfig> staticValidationConfig)
    {
        _logger = logger;
        _handleException = handleException;
        _createResponse = createResponse;
        _readRules = readRules;
        _httpClientFunction = httpClientFunction;
        _config = staticValidationConfig.Value;
    }

    // TODO: refactor to accept a cohort distribution participant
    [Function("StaticValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ParticipantCsvRecord participantCsvRecord;

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBodyJson);
            }

            var ruleFileName = $"{participantCsvRecord.Participant.ScreeningName}_staticRules.json".Replace(" ", "_");
            _logger.LogInformation("ruleFileName: {RuleFileName}", ruleFileName);

            var json = await _readRules.GetRulesFromDirectory(ruleFileName);
            var rules = JsonSerializer.Deserialize<Workflow[]>(json);

            var reSettings = new ReSettings
            {
                CustomTypes = [typeof(Regex), typeof(RegexOptions), typeof(ValidationHelper), typeof(Status), typeof(Actions)],
                UseFastExpressionCompiler = false
            };

            var re = new RulesEngine.RulesEngine(rules, reSettings);

            var ruleParameters = new[] {
                new RuleParameter("participant", participantCsvRecord.Participant),
            };
            var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

            if (re.GetAllRegisteredWorkflowNames().Contains(participantCsvRecord.Participant.RecordType))
            {
                _logger.LogInformation("Executing workflow {RecordType}", participantCsvRecord.Participant.RecordType);
                var ActionResults = await re.ExecuteAllRulesAsync(participantCsvRecord.Participant.RecordType, ruleParameters);
                resultList.AddRange(ActionResults);
            }

            var validationErrors = resultList.Where(x => !x.IsSuccess);

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
        await _httpClientFunction.SendPost(_config.RemoveOldValidationRecord, OldExceptionRecordJson);
    }
}
