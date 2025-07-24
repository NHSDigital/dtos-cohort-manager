namespace Common;

public interface IAuthorizationClientCredentials
{
    Task<string?> AccessToken(int expInMinutes = 1);
}