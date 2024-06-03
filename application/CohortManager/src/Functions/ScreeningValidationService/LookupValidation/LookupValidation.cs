namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;

public class LookupValidation
{
    private readonly ILogger<LookupValidation> _logger;
    private readonly IValidationData _createValidationData;

    public LookupValidation(ILogger<LookupValidation> logger, IValidationData createValidationData)
    {
        _logger = logger;
        _createValidationData = createValidationData;
    }

    [Function("LookupValidation")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string requestBodyJson;
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBodyJson = reader.ReadToEnd();
        }

        var requestBody = JsonSerializer.Deserialize<LookupValidationRequestBody>(requestBodyJson);

        if (requestBody is null)
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

                var dto = new ValidationDataDto
                {
                    RuleId = ruleDetails[0],
                    RuleName = ruleDetails[1],
                    Workflow = workflow,
                    NhsNumber = newParticipant.NHSId ?? null,
                    DateCreated = DateTime.UtcNow
                };

                _createValidationData.Create(dto);
            }
        }

        if (validationErrors.Count == 0)
        {
            return req.CreateResponse(HttpStatusCode.OK);
        }

        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}
