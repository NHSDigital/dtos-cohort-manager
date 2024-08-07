using System.Net;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GetLatestCohortDistributionRecord
{
    public class GetLatestCohortDistributionRecord
    {
        private readonly ILogger<GetLatestCohortDistributionRecord> _logger;
        private readonly ICreateResponse createResponse;

        public GetLatestCohortDistributionRecord(ILogger<GetLatestCohortDistributionRecord> logger, ICreateResponse createResponse)
        {
            _logger = logger;
        }

        [Function("GetlatestCohortDistributionRecord")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
