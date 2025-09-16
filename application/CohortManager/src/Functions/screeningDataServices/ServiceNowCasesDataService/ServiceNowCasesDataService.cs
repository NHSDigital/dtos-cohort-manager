namespace ServicenowCasesDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;
using Model;

public class ServiceNowCasesDataService
{
    private readonly ILogger<ServiceNowCasesDataService> _logger;
    private readonly IRequestHandler<ServicenowCase> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public ServiceNowCasesDataService(ILogger<ServiceNowCasesDataService> logger, IRequestHandler<ServicenowCase> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("ServiceNowCasesDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ServiceNowCasesDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} ", req.Method, typeof(NemsSubscription));
            var result = await _requestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }

}

