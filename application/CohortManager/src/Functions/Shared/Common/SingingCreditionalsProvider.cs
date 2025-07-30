namespace Common;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class SigningCredentialsProvider : ISigningCredentialsProvider
{
    private readonly JwtTokenServiceConfig _jwtTokenServiceConfig;
    private readonly JwtPrivateKey _jWTPrivateKey;

    private readonly ILogger<SigningCredentialsProvider> _logger;

    public SigningCredentialsProvider(IOptions<JwtTokenServiceConfig> jwtTokenServiceConfig, JwtPrivateKey jWTPrivateKey, ILogger<SigningCredentialsProvider> logger)
    {
        _logger = logger;
        _jWTPrivateKey = jWTPrivateKey;
        _jwtTokenServiceConfig = jwtTokenServiceConfig.Value;
    }

    public SigningCredentials CreateSigningCredentials()
    {
        try
        {
            _logger.LogInformation("attempting to  sanitize private key");
            var unescapedPrivateKey = SanitizePrivateKey(_jWTPrivateKey.PrivateKey);
            var keyBytes = Convert.FromBase64String(unescapedPrivateKey);

            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            _logger.LogInformation("successfully imported private key");

            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = _jwtTokenServiceConfig.KId
            };

            return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha512)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "there was an error while creating signing credentials");
            throw;
        }
    }

    private static string SanitizePrivateKey(string privateKey)
    {
        if (privateKey.Contains("BEGIN PRIVATE KEY"))
        {
            var sb = new StringBuilder(privateKey);
            sb.Replace("-----BEGIN PRIVATE KEY-----", "");
            sb.Replace("-----END PRIVATE KEY-----", "");
            sb.Replace("\t", "");
            sb.Replace("\n", "");
            sb.Replace("\r", "");
            return sb.ToString();
        }
        return privateKey;
    }

}

