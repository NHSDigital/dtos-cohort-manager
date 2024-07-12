namespace NHS.CohortManager.ParticipantDataService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class ExtractParticipant
{
    private readonly ILogger<ExtractParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly IUpdateParticipantData _getParticipantData;
    private readonly ICreateParticipant _createParticipant;

    public ExtractParticipant(ILogger<ExtractParticipant> logger, ICreateResponse createResponse, ICallFunction callFunction, IGetParticipantData getParticipantData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _getParticipantData = getParticipantData;
    }

    [Function("ExtractParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            var Id = req.Query["Id"];

            var participantData = _getParticipantData.GetParticipantData(Id);
            if (participantData == null)
            {
                _logger.LogInformation("participant function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            //CreateParticipant participant = _createParticipant.CreateResponseParticipantModel(participantData, demographicData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"There has been an error extracting data: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
