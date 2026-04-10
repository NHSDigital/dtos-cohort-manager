namespace Common;

using System.Text.Json;
using Hl7.FhirPath.Sprache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class Cis2UserService : ICis2UserService
{
    ILogger<Cis2UserService> _logger;
    IHttpClientFunction _httpClient;
    AuthConfig _authConfig;

    public Cis2UserService(ILogger<Cis2UserService> logger, IHttpClientFunction httpClient, IOptions<AuthConfig> authConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _authConfig = authConfig.Value;
    }

    public async Task<Cis2User?> GetUserFromToken(string token)
    {
        try{
            _httpClient.SetBearerToken(token);
            var response = await _httpClient.SendGetOrThrowAsync(_authConfig.UserInfoUrl);
            if(response == null)
            {
                _logger.LogError("Failed to get user info from token, response is null");
                return null;
            }
            var cis2User = JsonSerializer.Deserialize<Cis2User>(response);

            return cis2User;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to get user info from token, message: {Message}", ex.Message);
            return null;
        }
    }
}
