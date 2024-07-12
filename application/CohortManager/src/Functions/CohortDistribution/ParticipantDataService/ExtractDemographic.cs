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

public class ExtractDemographic
{
    private readonly ILogger<ExtractDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly IUpdateParticipantData _getParticipantData;
    private readonly ICreateParticipant _createParticipant;

    public ExtractDemographic(ILogger<ExtractDemographic> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
    }

    [Function("ExtractDemographic")]
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"There has been an error extracting data: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
