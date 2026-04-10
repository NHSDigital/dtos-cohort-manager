namespace Common;

public interface ICis2UserService
{
    /// <summary>
    /// Gets the user information from the token using the configured UserInfo endpoint.
    /// </summary>
    /// <param name="token">The token to get the user information from.</param>
    /// <returns>A Cis2User object containing the user information, or null if the user information could not be retrieved.</returns>
    Task<Cis2User?> GetUserFromToken(string token);
}
