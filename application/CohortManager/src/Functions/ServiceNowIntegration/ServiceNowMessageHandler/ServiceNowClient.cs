namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net.Http.Json;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ServiceNowIntegrationService.Models;

public class ServiceNowClient : IServiceNowClient
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ILogger<ServiceNowClient> _logger;
    private readonly ServiceNowMessageHandlerConfig _config;
    private const string TokenCacheKey = "AccessToken";

    public ServiceNowClient(IMemoryCache cache, IHttpClientFunction httpClientFunction,
        ILogger<ServiceNowClient> logger, IOptions<ServiceNowMessageHandlerConfig> config)
    {
        _cache = cache;
        _httpClientFunction = httpClientFunction;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<HttpResponseMessage?> SendUpdate(string sysId, ServiceNowUpdateRequestBody payload)
    {
        var url = $"{_config.ServiceNowUpdateUrl}/{sysId}";

        var json = JsonSerializer.Serialize(payload);

        var token = await GetAccessTokenAsync();

        if (token == null)
        {
            return null;
        }

        var response = await _httpClientFunction.SendServiceNowPut(url, token, json);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Token may have expired earlier than expected
            // Refresh the token and try once more
            token = await GetAccessTokenAsync();
            response = await _httpClientFunction.SendServiceNowPut(url, token, json);
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

        var response = await _httpClientFunction.SendServiceNowAccessTokenRefresh(
            _config.ServiceNowRefreshAccessTokenUrl, _config.ClientId, _config.ClientSecret, _config.RefreshToken);

        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh ServiceNow access token. StatusCode: {statusCode}", response?.StatusCode.ToString() ?? "Unknown");
            return null;
        }

        var responseBody = await response.Content.ReadFromJsonAsync<ServiceNowRefreshAccessTokenResponseBody>();

        return responseBody?.AccessToken;
    }
}
