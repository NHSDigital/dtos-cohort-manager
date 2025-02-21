namespace BsSelectRequestAuditDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Database;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;
using Model;

public class BsSelectRequestAuditDataService
{
    private readonly ILogger<BsSelectRequestAuditDataService> _logger;
    private readonly IRequestHandler<BsSelectRequestAudit> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public BsSelectRequestAuditDataService(ILogger<BsSelectRequestAuditDataService> logger, IRequestHandler<BsSelectRequestAudit> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("BsSelectRequestAuditDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "BsSelectRequestAuditDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} " ,req.Method,typeof(BsSelectRequestAudit));
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

