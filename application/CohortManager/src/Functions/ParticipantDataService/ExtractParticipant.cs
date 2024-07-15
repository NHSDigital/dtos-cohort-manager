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

    public ExtractParticipant(ILogger<ExtractParticipant> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic, IUpdateParticipantData getParticipantData, ICreateParticipant createParticipant)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _getParticipantData = getParticipantData;
        _createParticipant = createParticipant;
    }

    [Function("ExtractParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            var Id = req.Query["Id"];

            var demographicData = await _checkDemographic.GetDemographicAsync(Id, Environment.GetEnvironmentVariable("DemographicURIGet"));
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var participantData = _getParticipantData.GetParticipant(Id);
            if (participantData == null)
            {
                _logger.LogInformation("participant function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            //CreateParticipant participant = _createParticipant.CreateResponseParticipantModel(participantData, demographicData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"There has been an error exracting data: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
