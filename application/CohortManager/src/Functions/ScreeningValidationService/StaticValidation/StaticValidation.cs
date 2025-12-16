namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Runtime.CompilerServices;
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
using NHS.CohortManager.Shared.Utilities;
using RulesEngine.Models;

public class StaticValidation
{
    private readonly ILogger<StaticValidation> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IReadRules _readRules;
    private readonly IReasonForRemovalLookup _reasonForRemovalLookup;

    public StaticValidation(
        ILogger<StaticValidation> logger,
        ICreateResponse createResponse,
        IReadRules readRules,
        IReasonForRemovalLookup reasonForRemovalLookup)
    {
        _logger = logger;
        _createResponse = createResponse;
        _readRules = readRules;
        _reasonForRemovalLookup = reasonForRemovalLookup;
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

            bool routineParticipant = (participantCsvRecord.Participant.ReferralFlag ?? "").ToLower() == "false";
            bool isManualAdd = ValidationHelper.CheckManualAddFileName(participantCsvRecord.FileName);

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
                new RuleParameter("reasonForRemovalLkp", _reasonForRemovalLookup),
                new RuleParameter("manualAdd",isManualAdd)
            };

            var resultList = new List<RuleResultTree>();

            if (participantCsvRecord.Participant.RecordType != Actions.Removed)
            {
                resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

                if (routineParticipant)
                {
                    resultList.AddRange(await re.ExecuteAllRulesAsync("Routine_Common", ruleParameters));
                }
            }

            if (isManualAdd)
            {
                var manualAddResults = await re.ExecuteAllRulesAsync("Manual_Add", ruleParameters);
                resultList.AddRange(manualAddResults);
            }

            if (re.GetAllRegisteredWorkflowNames().Contains(participantCsvRecord.Participant.RecordType))
                {
                    _logger.LogInformation("Executing workflow {RecordType}", participantCsvRecord.Participant.RecordType);
                    var ActionResults = await re.ExecuteAllRulesAsync(participantCsvRecord.Participant.RecordType, ruleParameters);
                    resultList.AddRange(ActionResults);
                }

            var validationErrors = resultList.Where(x => !x.IsSuccess).Select(x => new ValidationRuleResult(x));

            if (validationErrors.Any())
            {
                string errors = JsonSerializer.Serialize(validationErrors);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, errors);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
