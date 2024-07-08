namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

    public class CreateException
    {
        private readonly ILogger<CreateException> _logger;

        public CreateException(ILogger<CreateException> logger)
        {
            _logger = logger;
        }

        [Function("CreateException")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            await Task.CompletedTask;
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }

