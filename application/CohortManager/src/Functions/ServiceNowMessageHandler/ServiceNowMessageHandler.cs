namespace NHS.CohortManager.ServiceNowIntegrationService;

using System;
using System.IO;
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

public class ServiceNowMsgHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceNowMsgHandler> _logger;
    private readonly SendServiceNowMsgConfig _config;
    private readonly ICreateResponse _createResponse;

    private static string? _cachedAccessToken;
    private static DateTime _lastTokenRefresh = DateTime.MinValue;
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromMinutes(55); // Assume 1hr expiry

    public ServiceNowMsgHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceNowMsgHandler> logger,
        IOptions<SendServiceNowMsgConfig> sendServiceNowMsgConfig,
        ICreateResponse createResponse)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = sendServiceNowMsgConfig.Value;
        _createResponse = createResponse;
    }

    [Function("ServiceNowMessageHandler")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", Route = "servicenow/{baseUrl?}/{profile?}/{sysId?}")] HttpRequestData req,
        string? baseUrl,
        string? profile,
        string? sysId)
    {
        _logger.LogInformation("Triggered ServiceNowMessageHandler with method: {Method}", req.Method);

        try
        {
            return req.Method.ToLower() switch
            {
                "get" or "post" => await HandleReceiveServiceNowMessage(req),
                "put" => await HandleSendServiceNowMessage(req, baseUrl, profile, sysId),
                _ => _createResponse.CreateHttpResponse(HttpStatusCode.MethodNotAllowed, req, $"HTTP method {req.Method} is not supported.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred in ServiceNowMessageHandler.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An unexpected error occurred.");
        }
    }

    private async Task<HttpResponseData> HandleReceiveServiceNowMessage(HttpRequestData req)
    {
        _logger.LogInformation("Handling receive ServiceNow message request.");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("Received body: {Body}", requestBody);
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, requestBody);
    }

    private async Task<HttpResponseData> HandleSendServiceNowMessage(HttpRequestData req, string? baseUrl, string? profile, string? sysId)
    {
        _logger.LogInformation("Handling send ServiceNow message request.");

        var requestBody = await req.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Request body is missing or empty.");

        var input = JsonSerializer.Deserialize<ServiceNowRequestModel>(requestBody);
        if (string.IsNullOrWhiteSpace(input?.WorkNotes))
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request payload.");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(profile) || string.IsNullOrWhiteSpace(sysId))
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Missing required route parameters.");

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

        _logger.LogInformation("Sending PUT request to ServiceNow URL: {Url}", url);

        var response = await _httpClient.PutAsync(url, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Token expired, attempting to refresh and retry...");
            token = await RefreshAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await _httpClient.PutAsync(url, content); // Retry once
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Received response: {StatusCode} - {Body}", response.StatusCode, responseBody);

        response.EnsureSuccessStatusCode();
        return _createResponse.CreateHttpResponse(response.StatusCode, req, responseBody);
    }

    private async Task<string> GetValidAccessTokenAsync()
    {
        if (_cachedAccessToken == null || DateTime.UtcNow - _lastTokenRefresh > TokenExpiryBuffer)
        {
            _logger.LogInformation("Refreshing access token...");
            _cachedAccessToken = await RefreshAccessTokenAsync();
        }

        return _cachedAccessToken!;
    }

    private async Task<string> RefreshAccessTokenAsync()
    {
        // NOTE: Replace this stub with a real API call to get a token from ServiceNow or identity provider
        await Task.Delay(500); // Simulate token fetch delay
        _lastTokenRefresh = DateTime.UtcNow;

        // Example: return await FetchNewTokenFromOAuthEndpoint();
        return _config.AccessToken; // fallback to config token for now
    }
}
