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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        Demographic participantDemographic = new Demographic();

        try
        {
            if (req.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    participantDemographic = JsonSerializer.Deserialize<Demographic>(requestBody);
                }

                var created = _createDemographicData.InsertDemographicData(participantDemographic);
                if (!created)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
                }
            }
            else
            {
                string Id = req.Query["Id"];

                var demographicData = _createDemographicData.GetDemographicData(Id);
                if (demographicData.ParticipantId != null)
                {
                    var responseBody = JsonSerializer.Serialize<Demographic>(demographicData);

                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
                }
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Participant not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error has occurred while inserting data {ex.Message}");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participantDemographic.NhsNumber, "N/A", "N/A", JsonSerializer.Serialize(participantDemographic));
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
