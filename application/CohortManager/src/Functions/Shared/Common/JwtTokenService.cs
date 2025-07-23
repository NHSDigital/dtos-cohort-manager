
namespace Common;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _audience;
    private readonly string _clientId;

    private readonly JwtTokenServiceConfig _jwtTokenServiceConfig;

    public JwtTokenService(IOptions<JwtTokenServiceConfig> jwtTokenServiceConfig)
    {
        _jwtTokenServiceConfig = jwtTokenServiceConfig.Value;
        _audience = _jwtTokenServiceConfig.Audience; // the NHS token service 
        _clientId = _jwtTokenServiceConfig.ClientId; // the API key in the hosted service in the NHS dev portal
    }

    /// <summary>
    /// generates a new JWT token signed with the private key 
    /// </summary>
    /// <param name="expInMinutes"></param>
    /// <returns></returns>
    public string GenerateJwt(int expInMinutes = 1)
    {
        var signingCredentials = CreateSigningCredentials();
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _clientId,
            audience: _audience,
            claims: new[] {
                new Claim("sub", _clientId),
                new Claim("jti", Guid.NewGuid().ToString())
            },
            notBefore: now,
            expires: now.AddMinutes(expInMinutes),
            signingCredentials: signingCredentials
        );
        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }

    private SigningCredentials CreateSigningCredentials()
    {
        var unescapedPrivateKey = SanitizePrivateKey(_jwtTokenServiceConfig.PrivateKey);
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