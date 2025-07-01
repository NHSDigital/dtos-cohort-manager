namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

public class ReceiveServiceNowMessageFunction
{
    private readonly ILogger<ReceiveServiceNowMessageFunction> _logger;
    private readonly ICreateResponse _createResponse;

    public ReceiveServiceNowMessageFunction(ILogger<ReceiveServiceNowMessageFunction> logger, ICreateResponse createResponse)
    {
        _logger = logger;
        _createResponse = createResponse;
    }

    /// <summary>
    /// Azure Function to receive and validate an incoming ServiceNow message.
    /// </summary>
    /// <param name="req">The HTTP request containing the incoming message payload.</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> object representing the HTTP response.
    /// - Returns a 202 status code if the request is successful.
    /// - Returns a 400 status code if the request is a bad request.
    /// </returns>
    [Function("ReceiveServiceNowMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "servicenow/receive")] HttpRequestData req)
    {
        ReceiveServiceNowMessageRequestBody? requestBody;

        try
        {
            requestBody = await JsonSerializer.DeserializeAsync<ReceiveServiceNowMessageRequestBody>(req.Body);

            if (requestBody == null)
            {
                _logger.LogError("Request body deserialised to null");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            var validationContext = new ValidationContext(requestBody);
            var validationResult = new List<ValidationResult>();
            bool isRequestBodyValid = Validator.TryValidateObject(requestBody, validationContext, validationResult, true);

            if (!isRequestBodyValid)
            {
                _logger.LogError("Request body failed validation");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize json request body to type {type}", nameof(ReceiveServiceNowMessageRequestBody));
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req);
    }
}
