namespace BsSelectOutCodeDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;

public class BsSelectOutCodeDataService
{
    private readonly ILogger<BsSelectOutCodeDataService> _logger;
    private readonly IRequestHandler<BsSelectOutCode> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public BsSelectOutCodeDataService(ILogger<BsSelectOutCodeDataService> logger, IRequestHandler<BsSelectOutCode> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("BsSelectOutCode")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
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

