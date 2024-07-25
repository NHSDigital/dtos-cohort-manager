namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Model.Enums;
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

        var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

        var transformedParticipant = new Participant()
        {
            FirstName = GetTransformedData<string>(resultList, "FirstName", participant.FirstName),
            Surname = GetTransformedData<string>(resultList, "Surname", participant.Surname),
            NhsNumber = GetTransformedData<string>(resultList, "NhsNumber", participant.NhsNumber),
            NamePrefix = GetTransformedData<string>(resultList, "NamePrefix", participant.NamePrefix),
            Gender = (Gender)GetTransformedData<int>(resultList, "Gender", Convert.ToInt32(participant.Gender))
        };

        var response = JsonSerializer.Serialize(transformedParticipant);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
    }

    private T GetTransformedData<T>(List<RuleResultTree> results, string field, T CurrentValue)
    {
        var result = results.Find(x => x.Rule.RuleName.Split('.')[1] == field);
        return result?.ActionResult?.Output == null ? CurrentValue : (T)result.ActionResult.Output;
    }
}
