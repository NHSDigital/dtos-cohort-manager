namespace Common;

using System.Security.Cryptography.X509Certificates;

public interface INemsHttpClientFunction : IHttpClientFunction
{
    /// <summary>
    /// Sends a POST request to NEMS API with proper authentication and headers
    /// </summary>
    /// <param name="url">NEMS API endpoint URL</param>
    /// <param name="subscriptionJson">FHIR subscription JSON</param>
    /// <param name="jwtToken">JWT bearer token</param>
    /// <param name="fromAsid">Source ASID</param>
    /// <param name="toAsid">Target ASID</param>
    /// <param name="clientCertificate">Client certificate for mutual TLS</param>
    /// <param name="bypassCertValidation">Whether to bypass certificate validation (DEBUG only)</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> SendSubscriptionPost(string url, string subscriptionJson, string jwtToken, string fromAsid, string toAsid, X509Certificate2? clientCertificate = null, bool bypassCertValidation = false);

    /// <summary>
    /// Sends a DELETE request to NEMS API with proper authentication and headers
    /// </summary>
    /// <param name="url">NEMS API endpoint URL</param>
    /// <param name="jwtToken">JWT bearer token</param>
    /// <param name="fromAsid">Source ASID</param>
    /// <param name="toAsid">Target ASID</param>
    /// <param name="clientCertificate">Client certificate for mutual TLS</param>
    /// <param name="bypassCertValidation">Whether to bypass certificate validation (DEBUG only)</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> SendSubscriptionDelete(string url, string jwtToken, string fromAsid, string toAsid, X509Certificate2? clientCertificate = null, bool bypassCertValidation = false);

    /// <summary>
    /// Generates an unsigned JWT token for NEMS API authentication
    /// </summary>
    /// <param name="asid">Your ASID</param>
    /// <param name="audience">The NEMS endpoint</param>
    /// <param name="scope">The required scope</param>
    /// <returns>Unsigned JWT token string</returns>
    string GenerateJwtToken(string asid, string audience, string scope);
}