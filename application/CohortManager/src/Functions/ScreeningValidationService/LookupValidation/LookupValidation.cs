namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Grpc.Net.Client.Balancer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Model;
using RulesEngine.Models;

public class LookupValidation
{
    private readonly ICallFunction _callFunction;
    private readonly IHandleException _handleException;

    private readonly ICreateResponse _createResponse;

    public LookupValidation(ICallFunction callFunction, ICreateResponse createResponse, IHandleException handleException)
    {
        _callFunction = callFunction;
        _createResponse = createResponse;
        _handleException = handleException;
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

        string json = File.ReadAllText("lookupRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);

        var reSettings = new ReSettings
        {
            CustomTypes = [typeof(Actions)]
        };

        var re = new RulesEngine.RulesEngine(rules, reSettings);

        var ruleParameters = new[] {
            new RuleParameter("existingParticipant", existingParticipant),
            new RuleParameter("newParticipant", newParticipant),
        };

        var resultList = await re.ExecuteAllRulesAsync("Common", ruleParameters);

        var validationErrors = resultList.Where(x => x.IsSuccess == false);

        if (validationErrors.Any())
        {

            var participantCsvRecord = new ParticipantCsvRecord()
            {
                Participant = newParticipant,
                FileName = requestBody.FileName
            };

            var updatedCsvRecord = await _handleException.CreateValidationExceptionLog(validationErrors, participantCsvRecord);
            var updatedCsvRecordJson = JsonSerializer.Serialize<ParticipantCsvRecord>(updatedCsvRecord);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, updatedCsvRecordJson);
        }

        if (validationErrors.Any())
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        return req.CreateResponse(HttpStatusCode.OK);
    }
}
