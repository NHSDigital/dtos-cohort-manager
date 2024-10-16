namespace BsSelectGpPractice;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Database;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using Common;

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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation($"Key Recieved: {key}");
            Func<BsSelectGpPractice, bool> predicate = null;
            if (key != null)
            {
                predicate = i => i.GpPracticeCode == key;
            }
            var result = await _requestHandler.HandleRequest(req, predicate);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }

}

