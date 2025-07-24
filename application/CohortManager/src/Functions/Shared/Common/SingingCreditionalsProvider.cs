using System.Security.Cryptography;
using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class SigningCredentialsProvider : ISigningCredentialsProvider
{
    private readonly JwtTokenServiceConfig _jwtTokenServiceConfig;
    private readonly JWTPrivateKey _jWTPrivateKey;

    public SigningCredentialsProvider(IOptions<JwtTokenServiceConfig> jwtTokenServiceConfig, JWTPrivateKey jWTPrivateKey)
    {
        _jWTPrivateKey = jWTPrivateKey;
        _jwtTokenServiceConfig = jwtTokenServiceConfig.Value;
    }

    public SigningCredentials CreateSigningCredentials()
    {
        var unescapedPrivateKey = SanitizePrivateKey(_jWTPrivateKey.PrivateKey);
        var keyBytes = Convert.FromBase64String(unescapedPrivateKey);

        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(keyBytes, out _);

        var rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = _jwtTokenServiceConfig.KId
        };

        return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha512)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }

    private static string SanitizePrivateKey(string privateKey)
    {
        var sb = new StringBuilder(privateKey);
        sb.Replace("-----BEGIN PRIVATE KEY-----", "");
        sb.Replace("-----END PRIVATE KEY-----", "");
        sb.Replace("\t", "");
        sb.Replace("\n", "");
        sb.Replace("\r", "");
        return sb.ToString();
    }

}

