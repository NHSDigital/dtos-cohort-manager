namespace Common;

using System.Net;
using System.Text.Json.Nodes;
using Apache.Arrow.Ipc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AuthClientCredentials : IAuthClientCredentials
{
    private readonly HttpClient _client;
    private readonly string _tokenUrl;
    private readonly IJwtTokenService _jwtHandler;
    private readonly JwtTokenServiceConfig _JwtTokenServiceConfig;

    private readonly ILogger<AuthClientCredentials> _logger;

    public AuthClientCredentials(IJwtTokenService jwtTokenService, HttpClient httpClient, IOptions<JwtTokenServiceConfig> JwtTokenServiceConfig, ILogger<AuthClientCredentials> logger)
    {
        _client = httpClient;
        _JwtTokenServiceConfig = JwtTokenServiceConfig.Value;

        _tokenUrl = _JwtTokenServiceConfig.AuthTokenURL;
        _jwtHandler = jwtTokenService;
        _logger = logger;

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

        var response = await _client.PostAsync(_tokenUrl, content);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError(await response.Content.ReadAsStringAsync());
            return null;
        }

        var resBody = await response.Content.ReadAsStringAsync();
        var parsed = JsonNode.Parse(resBody);

        return parsed?["access_token"]?.ToString();
    }
}
