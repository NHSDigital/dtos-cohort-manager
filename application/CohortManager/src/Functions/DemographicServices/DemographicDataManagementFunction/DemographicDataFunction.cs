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

public class DemographicDataFunction
{
    private readonly ILogger<DemographicDataFunction> _logger;
    private readonly ICreateResponse _createResponse;

    private readonly ICreateDemographicData _createDemographicData;

    public DemographicDataFunction(ILogger<DemographicDataFunction> logger, ICreateResponse createResponse, ICreateDemographicData createDemographicData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createDemographicData = createDemographicData;
    }

    [Function("DemographicDataFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            string NHSNumber = req.Query["Id"];

            var demographicData = await _createDemographicData.GetDemographicData(NHSNumber);
            var data = JsonSerializer.Serialize(demographicData);

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogInformation("Demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error saving demographic data: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
