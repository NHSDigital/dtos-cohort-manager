namespace NHS.CohortManager.ServiceNowIntegrationService;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ServiceNowMessageService.Models;
using Common;

public class SendServiceNowMessageFunction
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SendServiceNowMessageFunction> _logger;
    private readonly ServiceNowMessageHandlerConfig _config;
    private readonly ICreateResponse _createResponse;
    private string? _cachedAccessToken;
    private DateTime _lastTokenRefresh = DateTime.MinValue;
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(55);

    public SendServiceNowMessageFunction(
        IHttpClientFactory httpClientFactory,
        ILogger<SendServiceNowMessageFunction> logger,
        IOptions<ServiceNowMessageHandlerConfig> sendServiceNowMsgConfig,
        ICreateResponse createResponse)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = sendServiceNowMsgConfig.Value;
        _createResponse = createResponse;
    }

    /// <summary>
    /// Azure Function to send a message to the ServiceNow API, with automatic token refresh handling.
    /// </summary>
    /// <param name="req">The HTTP request containing the message to be sent.</param>
    /// <param name="baseUrl">Base URL of the ServiceNow instance.</param>
    /// <param name="profile">The profile environment (e.g. dev, test, prod).</param>
    /// <param name="sysId">The system identifier (sys_id) used for the PUT request path.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation:
    ///  - 200 OK on success
    ///  - 400 Bad Request for invalid input
    /// </returns>
    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "servicenow/send/{baseUrl}/{profile}/{sysId}")] HttpRequestData req,
        string baseUrl, string profile, string sysId)
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

        var url = $"https://{baseUrl}/{_config.EndpointPath}/{profile}/{_config.Definition}/{sysId}";

        var payload = new
        {
            state = input.State,
            work_notes = input.WorkNotes
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var token = await GetValidAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.PutAsync(url, content);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Token expired. Retrying with refreshed token.");
            token = await RefreshAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await _httpClient.PutAsync(url, content);
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
        return _config.AccessToken; // Replace with real token logic
    }
}
