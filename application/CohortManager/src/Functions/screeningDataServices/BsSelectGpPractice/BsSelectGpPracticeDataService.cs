namespace BsSelectGpPracticeDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;

public class BsSelectGpPracticeDataService
{
    private readonly ILogger<BsSelectGpPracticeDataService> _logger;
    private readonly IRequestHandler<BsSelectGpPractice> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public BsSelectGpPracticeDataService(ILogger<BsSelectGpPracticeDataService> logger, IRequestHandler<BsSelectGpPractice> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("BsSelectGpPracticeDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "BsSelectGpPracticeDataService/{*key}")] HttpRequestData req, string? key)
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

