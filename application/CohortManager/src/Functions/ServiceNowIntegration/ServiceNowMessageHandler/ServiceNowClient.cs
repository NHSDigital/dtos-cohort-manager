namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hl7.Fhir.ElementModel.Types;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ServiceNowIntegrationService.Models;

public class ServiceNowClient : IServiceNowClient
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceNowClient> _logger;
    private readonly ServiceNowMessageHandlerConfig _config;
    private const string AccessTokenCacheKey = "AccessToken";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ServiceNowClient(IMemoryCache cache, IHttpClientFactory httpClientFactory,
        ILogger<ServiceNowClient> logger, IOptions<ServiceNowMessageHandlerConfig> config)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<HttpResponseMessage?> SendUpdate(string caseNumber, string workNotes, bool needsAttention = false)
    {
        var url = $"{_config.ServiceNowUpdateUrl}/{caseNumber}";
        var payload = new ServiceNowUpdateRequestBody
        {
            State = 10, // 'Open' state
            WorkNotes = workNotes,
            NeedsAttention = needsAttention,
            AssignmentGroup = needsAttention ? _config.ServiceNowAssignmentGroup : null
        };

        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);

        return await SendRequest(url, json);
    }

    public async Task<HttpResponseMessage?> SendResolution(string caseNumber, string closeNotes)
    {
        var url = $"{_config.ServiceNowResolutionUrl}/{caseNumber}";
        var payload = new ServiceNowResolutionRequestBody
        {
            State = 6, // 'Resolved' state
            ResolutionCode = "28", // 'Solved by Automation' resolution code
            CloseNotes = closeNotes
        };
        var json = JsonSerializer.Serialize(payload, _jsonSerializerOptions);

        return await SendRequest(url, json);
    }

    private async Task<HttpResponseMessage?> SendRequest(string url, string json)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var token = await GetAccessTokenAsync();

        if (token == null)
        {
            _logger.LogError("Failed to get valid access token so exiting without sending request to ServiceNow.");
            return null;
        }

        var request = CreateRequest(url, json, token);

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Request returned an Unauthorized response, refreshing the access token and retrying the request...");
            token = await GetAccessTokenAsync(true);
            if (token == null)
            {
                _logger.LogError("Failed to get valid access token so exiting without sending request to ServiceNow.");
                return null;
            }
            var retryRequest = CreateRequest(url, json, token);
            response = await httpClient.SendAsync(retryRequest);
        }

        return response;
    }

    private static HttpRequestMessage CreateRequest(string url, string json, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    private async Task<string?> GetAccessTokenAsync(bool bypassCache = false)
    {
        if (!bypassCache && _cache.TryGetValue(AccessTokenCacheKey, out string? accessToken))
        {
            return accessToken;
        }

        _logger.LogInformation("Refreshing access token...");
        var response = await RefreshAccessTokenAsync();

        if (response == null)
        {
            return null;
        }

        var expiration = new TimeSpan(0, 0, response.ExpiresIn);

        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expiration);
        _cache.Set(AccessTokenCacheKey, response.AccessToken, cacheEntryOptions);

        _logger.LogInformation("Access token refreshed and stored in cache with an expiration of {expiration}", expiration);

        return response.AccessToken;
    }

    private async Task<ServiceNowRefreshAccessTokenResponseBody?> RefreshAccessTokenAsync()
    {
        var httpClient = _httpClientFactory.CreateClient();

        var dict = new Dictionary<string, string>
        {
            { "grant_type", _config.ServiceNowGrantType },
            { "client_id", _config.ServiceNowClientId },
            { "client_secret", _config.ServiceNowClientSecret }
        };

        if (_config.ServiceNowGrantType == "refresh_token" && _config.ServiceNowRefreshToken is not null)
        {
            dict.Add("refresh_token", _config.ServiceNowRefreshToken);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, _config.ServiceNowRefreshAccessTokenUrl)
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh ServiceNow access token. StatusCode: {statusCode}", response.StatusCode);
            return null;
        }

        var responseBody = await response.Content.ReadFromJsonAsync<ServiceNowRefreshAccessTokenResponseBody>();

        return responseBody;
    }
}
