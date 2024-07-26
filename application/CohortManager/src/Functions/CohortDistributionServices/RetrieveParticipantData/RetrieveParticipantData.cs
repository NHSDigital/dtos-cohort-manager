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
    private readonly ICreateDemographicData _createDemographicData;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger, ICallFunction callFunction, IUpdateParticipantData updateParticipantData, ICreateDemographicData createDemographicData)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _updateParticipantData = updateParticipantData;
        _createDemographicData = createDemographicData;
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
            var participantData = _updateParticipantData.GetParticipant(requestBody.NhsNumber);
            var demographicData = _createDemographicData.GetDemographicData(requestBody.NhsNumber);
            var responseBody = JsonSerializer.Serialize<Participant>(participantData);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError("Retrieve participant data failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }
    // task to call _createParticipant.CreateResponseParticipantModel (combine together)
}
