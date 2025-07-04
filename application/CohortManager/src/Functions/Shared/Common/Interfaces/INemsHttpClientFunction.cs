namespace Common;

using System.Security.Cryptography.X509Certificates;

public interface INemsHttpClientFunction
{
    /// <summary>
    /// Sends a POST request to NEMS API with proper authentication and headers
    /// </summary>
    /// <param name="request">NEMS subscription POST request object</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> SendSubscriptionPost(NemsSubscriptionPostRequest request);

    /// <summary>
    /// Sends a DELETE request to NEMS API with proper authentication and headers
    /// </summary>
    /// <param name="request">NEMS subscription DELETE request object</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> SendSubscriptionDelete(NemsSubscriptionRequest request);

    /// <summary>
    /// Generates an unsigned JWT token for NEMS API authentication
    /// </summary>
    /// <param name="asid">Your ASID</param>
    /// <param name="audience">The NEMS endpoint</param>
    /// <param name="scope">The required scope</param>
    /// <returns>Unsigned JWT token string</returns>
    string GenerateJwtToken(string asid, string audience, string scope);
}