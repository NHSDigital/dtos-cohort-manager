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
using Common.Interfaces;

public class LookupValidation
{
    private readonly IExceptionHandler _handleException;
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<LookupValidation> _logger;
    private readonly IReadRules _readRules;
    private readonly IDataLookupFacadeBreastScreening _dataLookup;

    public LookupValidation(
        ICreateResponse createResponse,
        IExceptionHandler handleException, ILogger<LookupValidation> logger,
        IDataLookupFacadeBreastScreening dataLookupFacade,
        IReadRules readRules
    )
    {
        _createResponse = createResponse;
        _handleException = handleException;
        _logger = logger;
        _dataLookup = dataLookupFacade;
        _readRules = readRules;
    }

    [Function("LookupValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        LookupValidationRequestBody requestBody = new LookupValidationRequestBody();

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = await reader.ReadToEndAsync();
                requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _handleException.CreateSystemExceptionLog(ex, requestBody.ExistingParticipant, requestBody.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        Participant newParticipant = null;

        try
        {
            newParticipant = requestBody.NewParticipant;

            var isManualAdd = CheckManualAddFileName(requestBody.FileName);

            var ruleFileName = $"{newParticipant.ScreeningName}_lookupRules.json".Replace(" ", "_");
            _logger.LogInformation("ruleFileName {RuleFileName}", ruleFileName);

            var json = await _readRules.GetRulesFromDirectory(ruleFileName);
            var rules = JsonSerializer.Deserialize<Workflow[]>(json);

            var reSettings = new ReSettings
            {
                CustomTypes = [typeof(Actions)],
                UseFastExpressionCompiler = false
            };
            var re = new RulesEngine.RulesEngine(rules, reSettings);





            var ruleParameters = new[] {
                new RuleParameter("existingParticipant", requestBody.ExistingParticipant),
                new RuleParameter("newParticipant", newParticipant),
                new RuleParameter("dbLookup", _dataLookup)
            };

            var resultList = new List<RuleResultTree>();

            if (newParticipant.RecordType != Actions.Removed)
            {
                resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);
            }

            if (re.GetAllRegisteredWorkflowNames().Contains(newParticipant.RecordType))
            {
                _logger.LogInformation("Executing workflow {RecordType}", newParticipant.RecordType);
                var ActionResults = await re.ExecuteAllRulesAsync(newParticipant.RecordType, ruleParameters);
                resultList.AddRange(ActionResults);
            }

            if (isManualAdd)
            {
                var ActionResults = await re.ExecuteAllRulesAsync("ManualAdd", ruleParameters);
                resultList.AddRange(ActionResults);
            }

            // Validation rules are logically reversed
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
            _handleException.CreateSystemExceptionLog(ex, newParticipant, "");
            _logger.LogError(ex, "Error while processing lookup Validation message: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);

        }
    }
    private static bool CheckManualAddFileName(string FileName)
    {
        if(string.IsNullOrEmpty(FileName))
        {
            return false;
        }
        if (FileName.ToLower().EndsWith(".parquet"))
        {
            return false;
        }
        return true;
    }
}
