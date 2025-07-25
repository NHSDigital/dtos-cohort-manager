
namespace Common;

public interface IBearerTokenService
{
    Task<string> GetBearerToken();
}