namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;

public class ServiceNowMessageHandler
{
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ILogger<ServiceNowMessageHandler> _logger;
    private readonly ServiceNowMessageHandlerConfig _config;
    private readonly ICreateResponse _createResponse;
    private string? _cachedAccessToken;
    private DateTime _lastTokenRefresh = DateTime.MinValue;
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(55);

    public ServiceNowMessageHandler(
        IHttpClientFunction httpClientFunction,
        ILogger<ServiceNowMessageHandler> logger,
        IOptions<ServiceNowMessageHandlerConfig> config,
        ICreateResponse createResponse)
    {
        _httpClientFunction = httpClientFunction;
        _logger = logger;
        _config = config.Value;
        _createResponse = createResponse;
    }

    /// <summary>
    /// Azure Function to send a message to the ServiceNow API, with automatic token refresh handling.
    /// </summary>
    /// <param name="req">The HTTP request containing the message to be sent.</param>
    /// <param name="baseUrl">Base URL of the ServiceNow instance.</param>
    /// <param name="profile">The profile environment (e.g., dev, test, prod).</param>
    /// <param name="sysId">The system identifier (sys_id) used for the PUT request path.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation:
    ///  - 200 OK on success
    ///  - 400 Bad Request for invalid input
    /// </returns>
    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> SendServiceNowMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "servicenow/send/{sysId}")] HttpRequestData req, string sysId)
    {
        _logger.LogInformation("SendServiceNowMessage function triggered.");

        var requestBody = await req.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(requestBody))
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body is missing or empty.");

        ServiceNowRequestModel? input;
        try
        {
            input = JsonSerializer.Deserialize<ServiceNowRequestModel>(requestBody);
        }
        catch
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "ServiceNow update failed.");
        }

        if (string.IsNullOrWhiteSpace(input?.WorkNotes))
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request payload.");

        var url = $"{_config.ServiceNowUpdateUrl}/{sysId}";

        var payload = new
        {
            state = input.State,
            work_notes = input.WorkNotes
        };

        var json = JsonSerializer.Serialize(payload);

        var token = await GetValidAccessTokenAsync();

        var response = await _httpClientFunction.SendServiceNowPut(url, token, json);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Token expired. Retrying with refreshed token.");
            token = await RefreshAccessTokenAsync();
            response = await _httpClientFunction.SendServiceNowPut(url, token, json);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("ServiceNow Response: {Code} - {Body}", response.StatusCode, responseBody);
        response.EnsureSuccessStatusCode();
        return _createResponse.CreateHttpResponse(response.StatusCode, req, responseBody);
    }

    private async Task<string> GetValidAccessTokenAsync()
    {
        if (_cachedAccessToken == null || DateTime.UtcNow - _lastTokenRefresh > TokenExpiryBuffer)
        {
            _cachedAccessToken = await RefreshAccessTokenAsync();
        }
        return _cachedAccessToken;
    }

    private async Task<string> RefreshAccessTokenAsync()
    {
        _logger.LogInformation("Refreshing access token...");
        await Task.Delay(500); // simulate external call
        _lastTokenRefresh = DateTime.UtcNow;
        return _config.AccessToken.ToString(); // Replace with real token logic
    }
}
