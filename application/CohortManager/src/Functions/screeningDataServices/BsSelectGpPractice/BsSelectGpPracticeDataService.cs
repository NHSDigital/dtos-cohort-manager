namespace BsSelectGpPractice;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DataServices.Database;
using System.Text.Json;

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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{*key}")] HttpRequestMessage req, string? key)
    {

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _logger.LogInformation($"Key Recieved: {key}");
        Func<BsSelectGpPractice,bool> predicate = null;
        if(key != null){
            predicate = i => i.GpPracticeCode == key;
        }
        var result = await _requestHandler.HandleRequest(req, predicate);
        return new OkObjectResult(result.JsonData);
    }

}

