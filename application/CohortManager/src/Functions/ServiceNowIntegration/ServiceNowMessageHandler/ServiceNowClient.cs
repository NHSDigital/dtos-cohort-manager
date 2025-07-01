namespace NHS.CohortManager.ServiceNowIntegrationService;

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
    private const string TokenCacheKey = "AccessToken";

    public ServiceNowClient(IMemoryCache cache, IHttpClientFactory httpClientFactory,
        ILogger<ServiceNowClient> logger, IOptions<ServiceNowMessageHandlerConfig> config)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Sends an HTTP request update a ServiceNow case.
    /// </summary>
    /// <param name="sysId">The ServiceNow case system identifier (sys_id) used in the HTTP request path.</param>
    /// <param name="payload">The ServiceNowUpdateRequestBody that will be sent in the HTTP request body.</param>
    /// <returns>
    /// An HTTP response indicating the result of the operation
    /// </returns>
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

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Token may have expired earlier than expected
            // Refresh the token and try once more
            token = await GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await httpClient.SendAsync(request);
        }

        return response;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (!_cache.TryGetValue(TokenCacheKey, out string? accessToken))
        {
            accessToken = await RefreshAccessTokenAsync();

            // ServiceNow access token is valid for 24 hours but setting to slightly below
            var expires = new TimeSpan(23, 55, 0);
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expires);

            _cache.Set(TokenCacheKey, accessToken, cacheEntryOptions);
        }

        return accessToken;
    }

    private async Task<string?> RefreshAccessTokenAsync()
    {
        _logger.LogInformation("Refreshing access token...");

        var httpClient = _httpClientFactory.CreateClient();

        var dict = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", _config.ClientId },
            { "client_secret", _config.ClientSecret },
            { "refresh_token", _config.RefreshToken }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _config.ServiceNowRefreshAccessTokenUrl)
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient.SendAsync(request);

        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh ServiceNow access token. StatusCode: {statusCode}", response?.StatusCode.ToString() ?? "Unknown");
            return null;
        }

        var responseBody = await response.Content.ReadFromJsonAsync<ServiceNowRefreshAccessTokenResponseBody>();

        return responseBody?.AccessToken;
    }
}
