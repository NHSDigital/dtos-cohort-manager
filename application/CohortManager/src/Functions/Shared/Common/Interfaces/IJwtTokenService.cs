
namespace Common;

public interface IJwtTokenService
{
    public string GenerateJwt(int expInMinutes = 1);
}
