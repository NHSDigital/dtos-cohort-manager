namespace Common;

using Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

class BearerTokenService : IBearerTokenService
{
    private readonly ILogger<BearerTokenService> _logger;
    private readonly IAuthorizationClientCredentials _authClientCredentials;
    private readonly IMemoryCache _memoryCache;
    private const string AccessTokenCacheKey = "AccessToken";


    public BearerTokenService(
        ILogger<BearerTokenService> logger,
        IAuthorizationClientCredentials authClientCredentials,
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
            _logger.LogInformation("bearer token found in memory cache");
            return bearerToken!;
        }

        _logger.LogInformation("Token not found in memory cache refreshing bearer token...");
        bearerToken = await _authClientCredentials.AccessToken();

        if (bearerToken == null)
        {
            return "";
        }
        //set time span to 10 seconds
        var expires = new TimeSpan(0, 10, 0);
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expires);
        _memoryCache.Set(AccessTokenCacheKey, bearerToken, cacheEntryOptions);


        _logger.LogInformation("Received access token");

        return bearerToken;
    }
}
