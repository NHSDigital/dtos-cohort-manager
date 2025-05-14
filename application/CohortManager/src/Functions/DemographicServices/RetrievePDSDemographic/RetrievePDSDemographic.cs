namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using NHS.Screening.RetrievePDSDemographic;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly RetrievePDSDemographicConfig _config;

    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger, 
        ICreateResponse createResponse, 
        ICallFunction callFunction,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _config = retrievePDSDemographicConfig.Value;
    }

    [Function("RetrievePdsDemographic")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            if (req.Query["participantId"] == null)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "No Participant ID Provided");
            }

            var pdsDemographicFunctionUrl = _config.ParticipantDemographicDataServiceURL;

            // Calling PDSDemographicDataFunction via ICallFunction
            var pdsDemographicResponseJson = await _callFunction.SendGet(pdsDemographicFunctionUrl);

            if (string.IsNullOrEmpty(pdsDemographicResponseJson))
            {
                _logger.LogError("Participant Not found");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Participant not found");
            }

            // Deserialize response to extract NHSNumber
            var demographicData = JsonSerializer.Deserialize<Demographic>(pdsDemographicResponseJson);

            if (demographicData != null && !string.IsNullOrEmpty(demographicData.NhsNumber))
            {
                _logger.LogInformation($"NHS Number found");
            }
            else
            {
                _logger.LogError("NHS Number not found in demographic response.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, pdsDemographicResponseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error fetching demographic data: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
// Model class for deserialization
public class Demographic
{
    public string? NhsNumber { get; set; }
}
