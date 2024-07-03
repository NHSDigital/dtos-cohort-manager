namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Common;

public class TransformDataService
{
    private readonly ICreateResponse _createResponse;
    public TransformDataService(ICreateResponse createResponse)
    {
        _createResponse = createResponse;
    }

    [Function("TransformDataService")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        TransformDataRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<TransformDataRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var participant = requestBody.Participant;

        // This function is currently not using the screeningService, but it will do in the future
        // var screeningService = requestBody.ScreeningService;

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

        var transformedParticipant = new Participant()
        {
            FirstName = HasTransformedData(transformations, "FirstName") ? GetTransformedData(transformations, "FirstName") : participant.FirstName,
            Surname = HasTransformedData(transformations, "Surname") ? GetTransformedData(transformations, "Surname") : participant.Surname,
            NhsNumber = HasTransformedData(transformations, "NhsNumber") ? GetTransformedData(transformations, "NhsNumber") : participant.NhsNumber,
            RecordType = HasTransformedData(transformations, "RecordType") ? GetTransformedData(transformations, "RecordType") : participant.RecordType,
            NamePrefix = HasTransformedData(transformations, "NamePrefix") ? GetTransformedData(transformations, "NamePrefix") : participant.NamePrefix
        };

        var response = JsonSerializer.Serialize(transformedParticipant);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
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
