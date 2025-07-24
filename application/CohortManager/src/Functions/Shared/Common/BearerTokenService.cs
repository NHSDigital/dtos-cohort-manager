namespace Common;

using Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

class BearerTokenService : IBearerTokenService
{
    private readonly ILogger<BearerTokenService> _logger;
    private readonly IAuthClientCredentials _authClientCredentials;
    private readonly IMemoryCache _memoryCache;
    private const string AccessTokenCacheKey = "AccessToken";


    public BearerTokenService(
        ILogger<BearerTokenService> logger,
        IAuthClientCredentials authClientCredentials,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _authClientCredentials = authClientCredentials;
        _memoryCache = memoryCache;
    }
    public async Task<string> GetBearerToken()
    {
        if (_memoryCache.TryGetValue(AccessTokenCacheKey, out string? bearerToken))
        {
            return bearerToken!;
        }

        _logger.LogInformation("Refreshing bearer token...");
        bearerToken = await _authClientCredentials.AccessToken();

        if (bearerToken == null)
        {
            return "";
        }

        var expires = new TimeSpan(0, 10, 0);
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expires);
        _memoryCache.Set(AccessTokenCacheKey, bearerToken, cacheEntryOptions);


        _logger.LogInformation("Received access token");

        return bearerToken;
    }
}
