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

    private readonly ICallFunction _callFunction;

    public LookupValidation(ICallFunction callFunction)
    {

        _callFunction = callFunction;
    }

    [Function("LookupValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        LookupValidationRequestBody requestBody;

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var existingParticipant = requestBody.ExistingParticipant;
        var newParticipant = requestBody.NewParticipant;
        var workflow = requestBody.Workflow;

        string json = File.ReadAllText("lookupRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var re = new RulesEngine.RulesEngine(rules);

        var ruleParameters = new[] {
            new RuleParameter("existingParticipant", existingParticipant),
            new RuleParameter("newParticipant", newParticipant),
        };

        var resultList = await re.ExecuteAllRulesAsync(workflow, ruleParameters);

        var validationErrors = new List<string>();

        foreach (var result in resultList)
        {
            if (!result.IsSuccess)
            {
                validationErrors.Add(result.Rule.RuleName);

                var ruleDetails = result.Rule.RuleName.Split('.');

                var exception = new ValidationException
                {
                    NhsNumber = newParticipant.NHSId ?? null,
                    DateCreated = DateTime.UtcNow,
                    FileName = requestBody.FileName,
                    RuleId = int.Parse(ruleDetails[0]),
                    DateResolved = DateTime.MaxValue,
                    RuleDescription = ruleDetails[1],
                    RuleContent = ruleDetails[1],
                    Category = 1,
                    ScreeningService = 1,
                    Cohort = null,
                    Fatal = 0
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
