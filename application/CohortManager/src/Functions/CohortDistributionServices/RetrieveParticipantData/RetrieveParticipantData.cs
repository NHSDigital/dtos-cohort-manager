namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using System.Text.Json;

public class RetrieveParticipantData
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RetrieveParticipantData> _logger;
    private readonly ICallFunction _callFunction;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger, ICallFunction callFunction)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
    }

    // this is a stub to return hardcoded participant data
    [Function("RetrieveParticipantData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        RetrieveParticipantRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<RetrieveParticipantRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var response = new CohortDistributionParticipant
        {
            NhsNumber = requestBody.NhsNumber,
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHH",
            Postcode = "NE63"
        };
        var responseBody = JsonSerializer.Serialize<CohortDistributionParticipant>(response);

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
    }
}
