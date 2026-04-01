namespace Common;

using Microsoft.Azure.Functions.Worker.Http;

public interface IAuthenticationService
{
    Task<bool> ValidateTokenAsync(string token);
}
