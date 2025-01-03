namespace ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class DemographicDataService
{
    private readonly ILogger<DemographicDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateDemographicData _createDemographicData;
    private readonly IExceptionHandler _exceptionHandler;

    public DemographicDataService(ILogger<DemographicDataService> logger, ICreateResponse createResponse, ICreateDemographicData createDemographicData, IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createDemographicData = createDemographicData;
        _exceptionHandler = exceptionHandler;
    }

    [Function("DemographicDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var participantDemographic = new List<Demographic>();

        try
        {
            string Id = req.Query["Id"];

            var demographicData = _createDemographicData.GetDemographicData(Id);
            if (demographicData != null)
            {
                var responseBody = JsonSerializer.Serialize<Demographic>(demographicData);

                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Participant not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error has occurred while inserting data {ex.Message}");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "N/A", "N/A", "N/A", JsonSerializer.Serialize(participantDemographic));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
