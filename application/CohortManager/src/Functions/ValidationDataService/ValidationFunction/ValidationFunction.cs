namespace ValidationDataService;

using System.Net;
using System.Text;
using System.Text.Json;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;

public class ValidationFunction
{
    private readonly ILogger<ValidationFunction> _logger;
    private readonly IValidationData _createValidationData;

    public ValidationFunction(ILogger<ValidationFunction> logger, IValidationData createValidationData)
    {
        _logger = logger;
        _createValidationData = createValidationData;
    }

    [Function("ValidationFunction")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var workflowName = "BreastCancerScreening";

        string requestBody;
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBody = reader.ReadToEnd();
        }

        var participantData = JsonSerializer.Deserialize<List<Participant>>(requestBody);

        if (participantData is null || participantData.Count != 2)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var existingParticipant = participantData[0];
        var newParticipant = participantData[1];

        string json = File.ReadAllText("rules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var re = new RulesEngine.RulesEngine(rules);

        var ruleParameters = new[] {
                new RuleParameter("existingParticipant", existingParticipant),
                new RuleParameter("newParticipant", newParticipant),
            };

        var resultList = await re.ExecuteAllRulesAsync(workflowName, ruleParameters);

        var validationErrors = new List<string>();

        foreach (var result in resultList)
        {
            if (!result.IsSuccess)
            {
                validationErrors.Add(result.Rule.RuleName);
                _createValidationData.UpdateRecords(new SQLReturnModel()
                {
                    commandType = CommandType.Command,
                    SQL = " INSERT INTO [dbo].[RULE_VIOLATED] ([RULE], TIME_VIOLATED, PARTICIPANT_ID) " +
                            " VALUES (@Rule_Violated, @TimeViolated, @ParticipantId) ",
                    parameters = new Dictionary<string, object>()
                        {
                            {"@Rule_Violated", result.Rule.RuleName },
                            {"@TimeViolated", DateTime.UtcNow },
                            {"@ParticipantId", existingParticipant.NHSId },
                        }
                });
            }
            _logger.LogInformation($"Rule - {result.Rule.RuleName}, IsSuccess - {result.IsSuccess}");
        }

        var httpStatusCode = validationErrors.Count == 0 ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        var response = req.CreateResponse(httpStatusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        byte[] data = Encoding.UTF8.GetBytes(string.Join(",", validationErrors));
        response.Body = new MemoryStream(data);

        return response;
    }
}
