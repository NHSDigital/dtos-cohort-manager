namespace BsSelectGpPractice;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Database;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;

public class BsSelectGpPracticeDataService
{
    private readonly ILogger<BsSelectGpPracticeDataService> _logger;
    private readonly IRequestHandler<BsSelectGpPractice> _requestHandler;

    public BsSelectGpPracticeDataService(ILogger<BsSelectGpPracticeDataService> logger, IRequestHandler<BsSelectGpPractice> requestHandler)
    {
        _logger = logger;
        _requestHandler = requestHandler;
    }

    [Function("BsSelectGpPracticeDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{*key}")] HttpRequestData req, string? key)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _logger.LogInformation($"Key Recieved: {key}");

        var result = await _requestHandler.HandleRequest(req, key);
        return CreateHttpResponse(HttpStatusCode.OK,req,result.JsonData);
    }

    private HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData req, string responseBody = "")
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        byte[] data = Encoding.UTF8.GetBytes(responseBody);

        response.Body = new MemoryStream(data);
        return response;
    }

}

