namespace NHS.CohortManager.CohortDistributionServices;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class RemoveCohortDistributionData
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RemoveCohortDistributionData> _logger;
    private readonly ICreateCohortDistributionData _createCohortDistributionData;

    public RemoveCohortDistributionData(ILogger<RemoveCohortDistributionData> logger, ICreateResponse createResponse, ICreateCohortDistributionData createCohortDistributionData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createCohortDistributionData = createCohortDistributionData;
    }

    [Function("RemoveCohortDistributionData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation($"C# HTTP trigger function processed a request");
        string nhsNumber = req.Query["NhsNumber"];

        if (string.IsNullOrEmpty(nhsNumber))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        var isUpdated = _createCohortDistributionData.UpdateCohortParticipantAsInactive(nhsNumber);

        if (!isUpdated)
        {
            _logger.LogInformation("No record could be removed there could have no record to remove or some other error could have happened");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }

        _logger.LogInformation("The cohort distribution record has been removed");
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
