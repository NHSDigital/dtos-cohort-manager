namespace NHS.CohortManager.ValidationDataService;

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

        var resultList = await re.ExecuteAllRulesAsync("Static", ruleParameters);

        var validationErrors = new List<string>();

        foreach (var result in resultList)
        {
            if (!result.IsSuccess)
            {
                validationErrors.Add(result.Rule.RuleName);

                var ruleDetails = result.Rule.RuleName.Split('.');
                _createValidationData.UpdateRecords(new SQLReturnModel()
                {
                    commandType = CommandType.Command,
                    SQL = " INSERT INTO [dbo].[RULE_VIOLATED] (RULE_ID], [DATE_CREATED],[NHS_ID],[DESCRIPTION],[RESOLVED]) " +
                            " VALUES (@Rule_ID, @Rule_Violated, @TimeViolated, @ParticipantId, @Description, @Resolved) ",
                    parameters = new Dictionary<string, object>()
                    {
                        {"@Rule_Violated", ruleDetails[0] },
                        {"@Rule_ID", ruleDetails[1]},
                        {"@NHS_Id", participant.NHSId ?? null },
                        {"Description", $"Rule - {result.Rule.RuleName}, IsSuccess - {result.IsSuccess}"},
                        {"@TimeViolated", DateTime.UtcNow },
                        {"@Resolved", result.IsSuccess }
                    }
                });
            }

            _logger.LogInformation($"Rule - {result.Rule.RuleName}, IsSuccess - {result.IsSuccess}");
        }

        var httpStatusCode = validationErrors.Count == 0 ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        var response = req.CreateResponse(httpStatusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        var errors = string.Join(",", validationErrors);
        byte[] data = Encoding.UTF8.GetBytes(errors);


        response.Body = new MemoryStream(data);

        return response;
    }
}
