namespace BsSelectGpPractice;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DataServices.Database;


public class BsSelectGpPracticeDataService
{
    private readonly ILogger<BsSelectGpPracticeDataService> _logger;
    private readonly IDataServiceAccessor<BsSelectGpPractice> _dataServiceAccessor;

    public BsSelectGpPracticeDataService(ILogger<BsSelectGpPracticeDataService> logger, IDataServiceAccessor<BsSelectGpPractice>)
    {
        _logger = logger;
    }

    [Function("BsSelectGpPracticeDataService")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{key:string?}")] HttpRequestMessage req, string? key)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        if(req.Method == HttpMethod.Get)
        {

        }
        return new OkObjectResult("Welcome to Azure Functions!");
    }

}

