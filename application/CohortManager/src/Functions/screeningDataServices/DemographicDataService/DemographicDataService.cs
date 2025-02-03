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

    private readonly ICreateResponse _createResponse;

    public DemographicDataService(ICreateResponse createResponse)
    {
        _createResponse = createResponse;
    }

    [Function("DemographicDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        return _createResponse.CreateHttpResponse(HttpStatusCode.Gone, req, "Participant not found");
    }
}
