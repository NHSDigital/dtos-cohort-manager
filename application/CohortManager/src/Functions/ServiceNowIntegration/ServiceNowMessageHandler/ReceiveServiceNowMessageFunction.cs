namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using System.Text.Json;
using System.Text;
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
    /// Azure Function to receive and log incoming ServiceNow messages.
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
        ReceiveServiceNowMessageRequestBody receiveServiceNowMessageRequest;

        try
        {
            receiveServiceNowMessageRequest = await JsonSerializer.DeserializeAsync<ReceiveServiceNowMessageRequestBody>(req.Body)
                ?? throw new ArgumentNullException(nameof(req.Body));

            var validationContext = new ValidationContext(receiveServiceNowMessageRequest);

            Validator.ValidateObject(receiveServiceNowMessageRequest, validationContext, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request body invalid");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req);
    }
}
