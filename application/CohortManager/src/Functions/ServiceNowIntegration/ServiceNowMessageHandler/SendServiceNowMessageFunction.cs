namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;
using System.Net.Http.Json;

public class SendServiceNowMessageFunction
{
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ILogger<SendServiceNowMessageFunction> _logger;
    private readonly ServiceNowMessageHandlerConfig _config;
    private readonly ICreateResponse _createResponse;
    private string? _cachedAccessToken;
    private DateTime _lastTokenRefresh;
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(55);

    public SendServiceNowMessageFunction(
        IHttpClientFunction httpClientFunction,
        ILogger<SendServiceNowMessageFunction> logger,
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
    /// <param name="sysId">The ServiceNow system identifier (sys_id) used in the PUT request path.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation:
    ///  - 200 OK for success
    ///  - 400 Bad Request for invalid input
    ///  - 500 Internal Server Error for unexpected exception
    /// </returns>
    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> Run(
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

        if (token == null)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

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

    private async Task<string?> GetValidAccessTokenAsync()
    {
        if (_cachedAccessToken == null || DateTime.UtcNow - _lastTokenRefresh > TokenExpiryBuffer)
        {
            _cachedAccessToken = await RefreshAccessTokenAsync();
            _lastTokenRefresh = DateTime.UtcNow;
        }
        return _cachedAccessToken;
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        _logger.LogInformation("Refreshing access token...");

        var response = await _httpClientFunction.SendServiceNowAccessTokenRefresh(
            _config.ServiceNowRefreshAccessTokenUrl, _config.ClientId, _config.ClientSecret, _config.RefreshToken);

        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh ServiceNow access token. StatusCode: {statusCode}", response?.StatusCode.ToString() ?? "Unknown");
            return null;
        }

        var responseBody = await response.Content.ReadFromJsonAsync<RefreshAccessTokenResponseBody>();

        return responseBody?.AccessToken;
    }
}
