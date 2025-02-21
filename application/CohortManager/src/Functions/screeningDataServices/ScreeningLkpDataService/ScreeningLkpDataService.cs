namespace ScreeningWorkflowDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;
using Model;

public class ScreeningLkpDataService
{
    private readonly ILogger<ScreeningLkpDataService> _logger;
    private readonly IRequestHandler<ScreeningLkp> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public ScreeningLkpDataService(ILogger<ScreeningLkpDataService> logger, IRequestHandler<ScreeningLkp> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("ScreeningLkpDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ScreeningLkpDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} " ,req.Method,typeof(ScreeningLkp));
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

