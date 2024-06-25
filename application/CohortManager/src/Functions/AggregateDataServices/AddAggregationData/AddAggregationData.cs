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

    [Function("AddAggregationData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            string requestBody = "";
            var participantCsvRecord = new AggregateParticipant();
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<AggregateParticipant>(requestBody);
            }

            var isAdded = _createAggregationData.InsertAggregationData(participantCsvRecord);
            if (isAdded)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
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
