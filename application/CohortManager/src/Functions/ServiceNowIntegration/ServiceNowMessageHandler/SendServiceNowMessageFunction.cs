namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using NHS.CohortManager.ServiceNowIntegrationService.Models;

public class SendServiceNowMessageFunction
{
    private readonly ILogger<SendServiceNowMessageFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IServiceNowClient _serviceNowClient;

    public SendServiceNowMessageFunction(ILogger<SendServiceNowMessageFunction> logger,
        ICreateResponse createResponse, IServiceNowClient serviceNowClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _serviceNowClient = serviceNowClient;
    }

    /// <summary>
    /// Azure Function to send a message to the ServiceNow API
    /// </summary>
    /// <param name="req">The HTTP request containing the message to be sent.</param>
    /// <param name="sysId">The ServiceNow case system identifier (sys_id) used in the HTTP request path.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation:
    ///  - 200 OK for success
    ///  - 400 Bad Request for invalid request body
    ///  - 500 Internal Server Error for unexpected exception
    /// </returns>
    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "servicenow/send/{sysId}")] HttpRequestData req, string sysId)
    {
        SendServiceNowMessageRequestBody? requestBody;

        try
        {
            requestBody = await JsonSerializer.DeserializeAsync<SendServiceNowMessageRequestBody>(req.Body);

            if (requestBody == null)
            {
                _logger.LogError("Request body deserialised to null");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize json request body to type {type}", nameof(SendServiceNowMessageRequestBody));
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        try
        {
            var payload = new ServiceNowUpdateRequestBody
            {
                State = requestBody.State,
                WorkNotes = requestBody.WorkNotes
            };

            var response = await _serviceNowClient.SendUpdate(sysId, payload);

            if (response == null || !response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update ServiceNow. StatusCode: {statusCode}", response?.StatusCode.ToString() ?? "Unknown");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
