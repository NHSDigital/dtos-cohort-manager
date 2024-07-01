namespace NHS.CohortManager.AggregationDataServices;

using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Model;


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
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "RemoveFromAggregate/{NHSID}")] HttpRequestData req, string NHSID)
    {
        _logger.LogInformation($"C# HTTP trigger function processed a request");

        if (NHSID.IsNullOrEmpty())
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        var isUpdated = _createAggregationData.UpdateAggregateParticipantAsInactive(NHSID);

        if (!isUpdated)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

    }
}
