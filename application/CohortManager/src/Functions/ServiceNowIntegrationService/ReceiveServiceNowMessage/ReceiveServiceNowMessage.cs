namespace NHS.CohortManager.ServiceNowIntegrationService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

public class ReceiveServiceNowMessage
{
    private readonly ILogger<ReceiveServiceNowMessage> _logger;

    private readonly ICreateResponse _createResponse;


    public ReceiveServiceNowMessage(ILogger<ReceiveServiceNowMessage> logger, ICreateResponse createResponse)
    {
        _logger = logger;
        _createResponse = createResponse;
    }

    [Function("ReceiveServiceNowMessage")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("ReceiveServiceNowMessage function processed a request.");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation(requestBody);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
