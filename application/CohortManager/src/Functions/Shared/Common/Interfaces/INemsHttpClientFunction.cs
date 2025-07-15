namespace Common;

using System.Security.Cryptography.X509Certificates;

public interface INemsHttpClientFunction
{
    /// <summary>
    /// Sends an HTTP POST request to the specified NEMS URL with the provided subscription JSON, headers, and authorization tokens.
    /// </summary>
    /// <param name="request">NEMS subscription POST request object containing the subscription JSON, authorization tokens, ASIDs, and certificates</param>
    /// <param name="timeoutSeconds">HTTP client timeout in seconds. Defaults to 300 seconds (5 minutes)</param>
    /// <returns>HTTP response message</returns>
    /// <remarks>
    /// This method sends a POST request to the specified NEMS endpoint with a JSON body and the necessary authorization headers.
    /// The request headers include:
    /// - <c>Authorization</c> with a Bearer token,
    /// - <c>fromASID</c> and <c>toASID</c> to specify the sender and receiver ASID values,
    /// - <c>InteractionID</c> to specify the interaction ID of the subscription creation process.
    /// The request uses mutual TLS authentication with client certificates for secure communication with NEMS.
    /// </remarks>
    Task<HttpResponseMessage> SendSubscriptionPost(NemsSubscriptionPostRequest request, int timeoutSeconds = 300);

    /// <summary>
    /// Sends an HTTP DELETE request to remove a NEMS subscription with proper authentication and headers.
    /// </summary>
    /// <param name="request">NEMS subscription DELETE request object containing the subscription URL, authorization tokens, ASIDs, and certificates</param>
    /// <param name="timeoutSeconds">HTTP client timeout in seconds. Defaults to 300 seconds (5 minutes)</param>
    /// <returns>HTTP response message</returns>
    /// <remarks>
    /// This method sends a DELETE request to the specified NEMS subscription endpoint with the necessary authorization headers.
    /// The request headers include:
    /// - <c>Authorization</c> with a Bearer token,
    /// - <c>fromASID</c> and <c>toASID</c> to specify the sender and receiver ASID values,
    /// - <c>InteractionID</c> to specify the interaction ID of the subscription deletion process.
    /// The request uses mutual TLS authentication with client certificates for secure communication with NEMS.
    /// </remarks>
    Task<HttpResponseMessage> SendSubscriptionDelete(NemsSubscriptionRequest request, int timeoutSeconds = 300);

    /// <summary>
    /// Generates an unsigned JWT token for NEMS API authentication with required claims and proper structure.
    /// </summary>
    /// <param name="asid">The ASID (Application Service Identifier) of the requesting system, used for both issuer and requesting_system claims</param>
    /// <param name="audience">The target NEMS endpoint URL that will validate this token</param>
    /// <param name="scope">The requested scope for the token (e.g., "patient/Subscription.write")</param>
    /// <returns>Base64-encoded unsigned JWT token string in the format "header.payload." (note the trailing dot for unsigned tokens)</returns>
    /// <remarks>
    /// This method generates a JWT token suitable for NEMS API authentication. The token includes:
    /// - Standard JWT claims (iss, sub, aud, exp, iat)
    /// - NEMS-specific claims (reason_for_request: "directcare", requesting_system)
    /// - No signature (unsigned token ending with empty signature section)
    /// The token is valid for 1 hour from the time of generation.
    /// </remarks>
    string GenerateJwtToken(string asid, string audience, string scope);
}