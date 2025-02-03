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
        return _createResponse.CreateHttpResponse(HttpStatusCode.Gone, req, "Participant not found");
    }
}
