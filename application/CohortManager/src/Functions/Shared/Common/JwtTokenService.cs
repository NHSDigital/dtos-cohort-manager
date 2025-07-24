
namespace Common;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtTokenServiceConfig _jwtTokenServiceConfig;
    private ISigningCredentialsProvider _signingCredentialsProvider;

    private readonly string _audience;
    private readonly string _clientId;


    public JwtTokenService(IOptions<JwtTokenServiceConfig> jwtTokenServiceConfig, ISigningCredentialsProvider signingCredentialsProvider)
    {
        _signingCredentialsProvider = signingCredentialsProvider;
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
        var signingCredentials = _signingCredentialsProvider.CreateSigningCredentials();
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _clientId,
            audience: _audience,
            claims: [
                new Claim("sub", _clientId),
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