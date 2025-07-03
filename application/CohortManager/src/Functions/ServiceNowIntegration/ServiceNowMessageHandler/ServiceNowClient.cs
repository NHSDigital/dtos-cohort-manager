namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    public ServiceNowClient(IMemoryCache cache, IHttpClientFactory httpClientFactory,
        ILogger<ServiceNowClient> logger, IOptions<ServiceNowMessageHandlerConfig> config)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<HttpResponseMessage?> SendUpdate(string sysId, ServiceNowUpdateRequestBody payload)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var url = $"{_config.ServiceNowUpdateUrl}/{sysId}";
        var json = JsonSerializer.Serialize(payload);
        var token = await GetAccessTokenAsync();

        if (token == null)
        {
            return null;
        }

        var request = CreateUpdateRequest(url, json, token);

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Update request returned an Unauthorized response, refreshing the access token and retrying the update request...");
            token = await GetAccessTokenAsync(true);
            if (token == null)
            {
                return null;
            }
            var retryRequest = CreateUpdateRequest(url, json, token);
            response = await httpClient.SendAsync(retryRequest);
        }

        return response;
    }

    private static HttpRequestMessage CreateUpdateRequest(string url, string json, string token)
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
        accessToken = await RefreshAccessTokenAsync();

        if (accessToken == null)
        {
            return null;
        }

        // ServiceNow access token is valid for 24 hours so setting cache expiry to slightly below
        var expires = new TimeSpan(23, 55, 0);
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expires);
        _cache.Set(AccessTokenCacheKey, accessToken, cacheEntryOptions);

        _logger.LogInformation("Access token refreshed and cache updated");

        return accessToken;
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        var httpClient = _httpClientFactory.CreateClient();

        var dict = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", _config.ServiceNowClientId },
            { "client_secret", _config.ServiceNowClientSecret },
            { "refresh_token", _config.ServiceNowRefreshToken }
        };

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

        return responseBody!.AccessToken;
    }
}
