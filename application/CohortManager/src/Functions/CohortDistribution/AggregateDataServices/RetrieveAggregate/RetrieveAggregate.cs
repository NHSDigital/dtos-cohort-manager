namespace NHS.CohortManager.AggregationDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class AddAggregationDataFunction
{
    private readonly ILogger<AddAggregationDataFunction> _logger;

    private readonly ICreateResponse _createResponse;

    private readonly ICreateAggregationData _createAggregationData;

    public AddAggregationDataFunction(ILogger<AddAggregationDataFunction> logger, ICreateAggregationData createAggregationData, ICreateResponse createResponse)
    {
        _logger = logger;
        _createAggregationData = createAggregationData;
        _createResponse = createResponse;
    }

    [Function("RetrieveAggregateData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            var aggregateParticipants = _createAggregationData.ExtractAggregateParticipants();
            if (aggregateParticipants != null)
            {
                var aggregateParticipantsJson = JsonSerializer.Serialize(aggregateParticipants);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, aggregateParticipantsJson);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
