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

    public StaticValidation(ILogger<StaticValidation> logger, ICallFunction callFunction)
    {
        _logger = logger;
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
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        string json = File.ReadAllText("staticRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var reSettings = new ReSettings
        {
            CustomTypes = [typeof(Regex), typeof(RegexOptions), typeof(Validators), typeof(Status)]
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participantCsvRecord.Participant),
        };

        var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

        var validationErrors = resultList.Where(x => x.IsSuccess == false);

        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');

            var exception = new ValidationException
            {
                RuleId = int.Parse(ruleDetails[0]),
                RuleDescription = ruleDetails[1],
                RuleContent = ruleDetails[1],
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                Category = 1,
                ScreeningService = 1,
                Cohort = "",
                Fatal = 0
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateValidationExceptionURL"), exceptionJson);
        }

        if (validationErrors.Any())
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
