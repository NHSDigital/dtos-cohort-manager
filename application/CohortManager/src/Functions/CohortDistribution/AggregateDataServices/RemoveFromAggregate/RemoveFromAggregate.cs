namespace NHS.CohortManager.AggregationDataServices;

using System.Net;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class RemoveFromAggregate
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RemoveFromAggregate> _logger;
    private readonly ICreateAggregationData _createAggregationData;

    public RemoveFromAggregate(ILogger<RemoveFromAggregate> logger, ICreateResponse createResponse, ICreateAggregationData updateAggregateData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createAggregationData = _createAggregationData;
    }

    [Function("RemoveFromAggregateData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation($"C# HTTP trigger function processed a request");
        string nhsNumber = req.Query["NhsNumber"];

        if (string.IsNullOrEmpty(nhsNumber))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        var isUpdated = _createAggregationData.UpdateAggregateParticipantAsInactive(nhsNumber);

        if (!isUpdated)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

    }
}
