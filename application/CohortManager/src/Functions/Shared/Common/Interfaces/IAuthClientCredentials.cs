namespace Common;

public interface IAuthClientCredentials
{
    Task<string?> AccessToken(int expInMinutes = 1);
}