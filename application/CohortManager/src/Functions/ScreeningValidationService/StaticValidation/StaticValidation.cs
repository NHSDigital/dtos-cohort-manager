namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;

public class StaticValidation
{
    private readonly ILogger< StaticValidation> _logger;
    private readonly IValidationData _createValidationData;

    public  StaticValidation(ILogger< StaticValidation> logger, IValidationData createValidationData)
    {
        _logger = logger;
        _createValidationData = createValidationData;
    }

    [Function("StaticValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string requestBodyJson;
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBodyJson = reader.ReadToEnd();
        }

        var participant = JsonSerializer.Deserialize<Participant>(requestBodyJson);
        var workflow = "Static";

        if (participant is null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        string json = File.ReadAllText("staticRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var reSettings = new ReSettings{
            CustomTypes = [typeof(Regex)]
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
        };

        var resultList = await re.ExecuteAllRulesAsync(workflow, ruleParameters);

        var validationErrors = new List<string>();

        foreach (var result in resultList)
        {
            if (!result.IsSuccess)
            {
                validationErrors.Add(result.Rule.RuleName);

                var ruleDetails = result.Rule.RuleName.Split('.');

                var dto = new ValidationDataDto
                {
                    RuleId = ruleDetails[0],
                    RuleName = ruleDetails[1],
                    Workflow = workflow,
                    NhsNumber = participant.NHSId ?? null,
                    DateCreated = DateTime.UtcNow
                };

                _createValidationData.Create(dto);
            }
        }

        if (validationErrors.Count == 0)
        {
            return req.CreateResponse(HttpStatusCode.OK);
        }

        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        var errors = string.Join(",", validationErrors);
        byte[] data = Encoding.UTF8.GetBytes(errors);
        response.Body = new MemoryStream(data);

        return response;
    }
}
