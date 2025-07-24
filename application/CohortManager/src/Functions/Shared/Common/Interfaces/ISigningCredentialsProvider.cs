
namespace Common;

using Microsoft.IdentityModel.Tokens;

public interface ISigningCredentialsProvider
{
    SigningCredentials CreateSigningCredentials();
}