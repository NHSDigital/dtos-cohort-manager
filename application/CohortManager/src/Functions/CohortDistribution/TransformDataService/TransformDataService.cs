namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using Model;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    public TransformDataService(ILogger<TransformDataService> logger)
    {
        _logger = logger;
    }

    [Function("TransformDataService")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NhsNumber = "1",
            RecordType = Actions.New,
            NamePrefix = "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHH",
        };

        string json = await File.ReadAllTextAsync("transformRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var re = new RulesEngine.RulesEngine(rules);

        var ruleParameters = new[] {
            new RuleParameter("participant", participant),
        };

        var ruleResultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        var transformations = new List<string>();

        foreach (var result in ruleResultList)
        {
            if (!result.IsSuccess)
            {
                var fieldName = result.Rule.RuleName.Split('.')[1];
                var output = result.ActionResult.Output;

                transformations.Add($"{fieldName}:{output}");
            }
        }

        var newParticipant = new Participant()
        {
            FirstName = HasTransformedData(transformations, "FirstName") ? GetTransformedData(transformations, "FirstName") : participant.FirstName,
            Surname = HasTransformedData(transformations, "Surname") ? GetTransformedData(transformations, "Surname") : participant.Surname,
            NhsNumber = HasTransformedData(transformations, "NhsNumber") ? GetTransformedData(transformations, "NhsNumber") : participant.NhsNumber,
            RecordType = HasTransformedData(transformations, "RecordType") ? GetTransformedData(transformations, "RecordType") : participant.RecordType,
            NamePrefix = HasTransformedData(transformations, "NamePrefix") ? GetTransformedData(transformations, "NamePrefix") : participant.NamePrefix
        };

        Console.WriteLine($"transformations: {JsonSerializer.Serialize(transformations)}");
        Console.WriteLine($"newParticipant: {JsonSerializer.Serialize(newParticipant)}");
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private bool HasTransformedData(List<string> data, string field)
    {
        return data.Exists(item => item.Contains(field));
    }

    private string GetTransformedData(List<string> data, string field)
    {
        return data.Find(item => item.Contains(field)).Split(":")[1];
    }
}
