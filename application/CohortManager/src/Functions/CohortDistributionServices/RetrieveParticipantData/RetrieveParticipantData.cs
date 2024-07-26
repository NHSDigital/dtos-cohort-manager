namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using System.Text.Json;
using Data.Database;

public class RetrieveParticipantData
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RetrieveParticipantData> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IUpdateParticipantData _updateParticipantData;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger, ICallFunction callFunction, IUpdateParticipantData updateParticipantData)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _updateParticipantData = updateParticipantData;
    }

    [Function("RetrieveParticipantData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        RetrieveParticipantRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<RetrieveParticipantRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        try
        {
            var participantData = await ExtractParticipant(requestBody.NhsNumber);
            var responseBody = JsonSerializer.Serialize<Participant>(participantData);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError("Retrieve participant data failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }

    private async Task<Participant> ExtractParticipant(string nhsNumber)
    {
        var participantData = _updateParticipantData.GetParticipant(nhsNumber);

        if (participantData != null)
        {
            return participantData;
        }
        else
        {
            throw new Exception("error");
        }
    }

    // task to call _createDemographicData.GetDemographicData (extract demographics)

    // task to call _createParticipant.CreateResponseParticipantModel (combine together)
}
