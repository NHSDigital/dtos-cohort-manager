namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Model;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    public RetrievePdsDemographic(ILogger<RetrievePdsDemographic> logger, ICreateResponse createResponse, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
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

            var participantId = req.Query["participantId"]!;
            var pdsDemographicFunctionUrl = $"https://pds-api-endpoint/PDSDemographicDataFunction?participantId={participantId}";

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
