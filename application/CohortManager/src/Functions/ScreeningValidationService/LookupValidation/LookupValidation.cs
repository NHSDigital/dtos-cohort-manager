namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = reader.ReadToEnd();
                requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);
            }
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

        var validationErrors = resultList.Where(x => x.IsSuccess == false);

        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');

            var exception = new ValidationException
            {
                RuleId = int.Parse(ruleDetails[0]),
                RuleDescription = ruleDetails[1],
                RuleContent = ruleDetails[1],
                FileName = requestBody.FileName,
                NhsNumber = newParticipant.NhsNumber ?? null,
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                Category = 1,
                ScreeningService = 1,
                Cohort = null,
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
