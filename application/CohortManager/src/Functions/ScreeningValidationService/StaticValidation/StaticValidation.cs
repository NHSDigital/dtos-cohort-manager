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
using RulesEngine.Models;

public class StaticValidation
{
    private readonly ILogger<StaticValidation> _logger;
    private readonly ICallFunction _callFunction;
    private ParticipantCsvRecord _participantCsvRecord;

    public StaticValidation(ILogger<StaticValidation> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    [Function("StaticValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        // Set which ruleset to use, needs to be parameterised
        var screeningService = 1;

        // Deserialisation
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            _participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        // Get rules
        string json = File.ReadAllText("staticRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var reSettings = new ReSettings
        {
            CustomTypes = [typeof(Regex)]
        };

        // Validation
        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", _participantCsvRecord.Participant),
        };

        var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

        var validationErrors = new List<string>();

        // Validation errors
        foreach (var result in resultList)
        {
            if (!result.IsSuccess)
            {
                validationErrors.Add(result.Rule.RuleName);

                var ruleDetails = result.Rule.RuleName.Split('.');
                System.Console.WriteLine(result.Rule);

                var exception = new ValidationException
                {
                    FileName = _participantCsvRecord.FileName,
                    NhsNumber = _participantCsvRecord.Participant.NHSId ?? null,
                    DateCreated = DateTime.UtcNow,
                    RuleDescription = ruleDetails[1],
                    RuleContent = ruleDetails[1],
                    DateResolved = DateTime.MaxValue,
                    ScreeningService = screeningService,
                };

                var exceptionJson = JsonSerializer.Serialize(exception);
                await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateValidationExceptionURL"), exceptionJson);
            }
        }

        if (validationErrors.Count == 0)
        {
            return req.CreateResponse(HttpStatusCode.OK);
        }

        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}
