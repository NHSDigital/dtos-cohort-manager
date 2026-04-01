namespace Common;

using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Options;
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
            _authConfig.AuthMetaDataUrl,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever()
        );
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token is missing");
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Token is not a valid JWT format");
                return false;
            }

            var oidcConfig = await _configurationManager.GetConfigurationAsync();
            var validatorParam = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = oidcConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = _authConfig.AuthClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            _ = handler.ValidateToken(token, validatorParam, out _);
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
