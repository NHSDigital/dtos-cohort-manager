namespace Common;

using System.Net;
using System.Text.Json.Nodes;
using Apache.Arrow.Ipc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AuthorizationClientCredentials : IAuthorizationClientCredentials
{
    private readonly HttpClient _httpClient;
    private readonly IJwtTokenService _jwtHandler;
    private readonly JwtTokenServiceConfig _JwtTokenServiceConfig;

    private readonly ILogger<AuthorizationClientCredentials> _logger;

    public AuthorizationClientCredentials(IJwtTokenService jwtTokenService, HttpClient httpClient, IOptions<JwtTokenServiceConfig> JwtTokenServiceConfig, ILogger<AuthorizationClientCredentials> logger)
    {
        _httpClient = httpClient;
        _jwtHandler = jwtTokenService;
        _logger = logger;

        _JwtTokenServiceConfig = JwtTokenServiceConfig.Value;
    }

    public async Task<string?> AccessToken(int expInMinutes = 1)
    {
        var jwt = _jwtHandler.GenerateJwt(expInMinutes);

        var values = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"},
            {"client_assertion", jwt}
        };
        var content = new FormUrlEncodedContent(values);

        var response = await _httpClient.PostAsync(_JwtTokenServiceConfig.AuthTokenURL, content);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var resBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonNode.Parse(resBody);

        return parsed?["access_token"]?.ToString();
    }
}
