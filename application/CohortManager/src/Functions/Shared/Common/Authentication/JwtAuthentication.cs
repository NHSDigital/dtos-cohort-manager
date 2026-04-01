namespace Common;

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Options;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData; // Alias to avoid confusion with Microsoft.IdentityModel.Protocols
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;

public class JWTAuthentication : IAuthenticationService
{
    private readonly ILogger<JWTAuthentication> _logger;
    private readonly AuthConfig _authConfig;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;


    public JWTAuthentication(IOptions<AuthConfig> authConfig, ILogger<JWTAuthentication> logger)
    {
        _authConfig = authConfig.Value;
        _logger = logger;
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _authConfig.MetaDataUrl,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever()
        );
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var oidcConfig = await _configurationManager.GetConfigurationAsync();
            var validatorParam = new TokenValidationParameters
            {
                ValidIssuer = oidcConfig.Issuer,
                ValidAudience = _authConfig.ClientId,
                IssuerSigningKeys = oidcConfig.SigningKeys
            };

            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validatorParam, out var validatedToken);
            return true;

        }
        catch(SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            return false;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during token validation");
            return false;
        }
    }

}
