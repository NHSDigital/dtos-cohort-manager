namespace BsSelectGpPractice;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;



public class BsSelectGpPractice
{
    private readonly ILogger<BsSelectGpPractice> _logger;

    public BsSelectGpPractice(ILogger<BsSelectGpPractice> logger)
    {
        _logger = logger;
    }

    [Function("BsSelectGpPractice")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "{key:string?}")] HttpRequestMessage req, string? key)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        switch(req.Method)
        {
            case HttpMethod.Get:


        }
        return new OkObjectResult("Welcome to Azure Functions!");
    }

}

