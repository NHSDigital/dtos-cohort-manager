
namespace Common;

using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtTokenServiceConfig _jwtTokenServiceConfig;
    private readonly ISigningCredentialsProvider _signingCredentialsProvider;

    public JwtTokenService(IOptions<JwtTokenServiceConfig> jwtTokenServiceConfig, ISigningCredentialsProvider signingCredentialsProvider)
    {
        _signingCredentialsProvider = signingCredentialsProvider;
        _jwtTokenServiceConfig = jwtTokenServiceConfig.Value;
    }

    /// <summary>
    /// generates a new JWT token signed with the private key 
    /// </summary>
    /// <param name="expInMinutes"></param>
    /// <returns></returns>
    public string GenerateJwt(int expInMinutes = 1)
    {
        var signingCredentials = _signingCredentialsProvider.CreateSigningCredentials();
        var now = DateTime.UtcNow;

        var clientId = _jwtTokenServiceConfig.ClientId;
        var audience = _jwtTokenServiceConfig.Audience;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException("The client id or audience was null");
        }

        var token = new JwtSecurityToken(
            issuer: clientId,
            audience: audience,
            claims: [
                new Claim("sub", _jwtTokenServiceConfig.ClientId),
                new Claim("jti", Guid.NewGuid().ToString())
            ],
            notBefore: now,
            expires: now.AddMinutes(expInMinutes),
            signingCredentials: signingCredentials
        );
        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }



}