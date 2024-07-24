namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Common;
using System.Collections;
using System.Text.RegularExpressions;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.OpenApi.Models;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.AspNetCore.Http;

public class TransformDataService
{
    private readonly ICreateResponse _createResponse;
    public TransformDataService(ICreateResponse createResponse)
    {
        _createResponse = createResponse;
    }

    [Function("TransformDataService")]
    // [OpenApiOperation(operationId: "greeting", tags: new[] { "greeting" }, Summary = "Greetings", Description = "This shows a welcome message.", Visibility = OpenApiVisibilityType.Important)]
    // [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Summary = "The response", Description = "This returns the response")]
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
            FirstName = GetTransformedData(resultList, "FirstName") ?? participant.FirstName,
            Surname = GetTransformedData(resultList, "Surname") ?? participant.Surname,
            NhsNumber = GetTransformedData(resultList, "NhsNumber") ?? participant.NhsNumber,
            NamePrefix = GetTransformedData(resultList, "NamePrefix") ?? participant.NamePrefix
        };

        var pparticipant = TransformNamePrefix(transformedParticipant);

        var response = JsonSerializer.Serialize(transformedParticipant);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
    }

    private string GetTransformedData(List<RuleResultTree> results, string field)
    {
        return (string)results.Find(x => x.Rule.RuleName.Split('.')[1] == field)?.ActionResult.Output;
    }

    public async Task<Participant> TransformNamePrefix(Participant participant) {

        var ruleTable = new Dictionary<string, string>
            {
                {"37.NamePrefix.AirMarshal", "A.ML"},
                {"38.NamePrefix.Admiral", "ADM"},
                {"39.NamePrefix.Brigadier", "BRIG"},
                {"40.NamePrefix.Brother", "BRO"}, {"41.NamePrefix.Br", "BRO"},
                {"42.NamePrefix.Captain", "CAPT"}, {"43.NamePrefix.Cpt", "CAPT"},
                {"44.NamePrefix.Commander", "CMDR"}, {"45.NamePrefix.Cdr", "CMDR"},
                {"46.NamePrefix.Colonel", "COL"}, {"48.NamePrefix.Doctor", "DR"},
                {"49.NamePrefix.Father", "FR"},{"50.NamePrefix.Fath", "FR"},
                {"51.NamePrefix.General", "GEN"}, {"52.NamePrefix.Honourable", "HON"},
                {"53.NamePrefix.Judge", "HON"}, {"54.NamePrefix.HisRoyalHighness", "HRH"},
                {"55.NamePrefix.HerRoyalHighness", "HRH"},{"56.NamePrefix.Baroness", "LADY"},
                {"57.NamePrefix.Baron", "LORD"}, {"58.NamePrefix.Duchess.", "LADY"},
                {"59.NamePrefix.Duke.", "LORD"}, {"60.NamePrefix.LieutenantColonel", "LT.C"},
                {"61.NamePrefix.Major.", "MAJ"}, {"62.NamePrefix.Master.", "MR"},
                {"63.NamePrefix.Mast", "MR"}, {"64.NamePrefix.Mister", "MR"},
                {"65.NamePrefix.Mstr", "MR"}, {"67.NamePrefix.Count", "R.HN"},
                {"68.NamePrefix.Countess", "R.HN"}, {"72.NamePrefix.Bishop", "R.RV"},
                {"76.NamePrefix.Mosignor", "REV"}, {"77.NamePrefix.Pastor", "REV"},
                {"80.NamePrefix.Sister", "SR"}, {"82.NamePrefix.WingCommander", "WCOM"}
            };

        string json = File.ReadAllText("namePrefixRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var re = new RulesEngine.RulesEngine(rules);

        // var reSettings = new ReSettings{
		// 	CustomTypes = new Type[]{typeof(Regex)}
		// };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("NamePrefix");//, reSettings);
        System.Console.WriteLine("participant title: " + participant.NamePrefix);
        System.Console.WriteLine("rules executed: " + GetTransformedData(rulesList, "NamePrefix")); 
        // turn list into string

        // Apply name prefix transformation
        //participant.NamePrefix = ruleTable["*RULE NAME*"];

        return participant;
    }
}
