namespace Common;

using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// NEMS-specific HTTP client function for managing NHS Event Management Service subscriptions.
/// Provides specialized operations for NEMS API interactions including client certificates, 
/// JWT token authentication, and FHIR-compliant subscription management.
/// </summary>
public class NemsHttpClientFunction : INemsHttpClientFunction
{
    private readonly ILogger<NemsHttpClientFunction> _logger;
    private readonly INemsHttpClientProvider _nemsHttpClientProvider;

    public NemsHttpClientFunction(
        ILogger<NemsHttpClientFunction> logger,
        INemsHttpClientProvider nemsHttpClientProvider)
    {
        _logger = logger;
        _nemsHttpClientProvider = nemsHttpClientProvider;
    }

    /// <summary>
    /// Sends a POST request to create a NEMS subscription with proper authentication and headers.
    /// </summary>
    /// <param name="request">NEMS subscription POST request object</param>
    /// <param name="timeoutSeconds">HTTP client timeout in seconds. Defaults to 300 seconds (5 minutes)</param>
    /// <returns>HTTP response from NEMS API</returns>
    public async Task<HttpResponseMessage> SendSubscriptionPost(NemsSubscriptionPostRequest request, int timeoutSeconds = 300)
    {
        var client = _nemsHttpClientProvider.CreateClient(request.ClientCertificate, request.BypassCertValidation);
        client.BaseAddress = new Uri(request.Url);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Url)
        {
            Content = new StringContent(request.SubscriptionJson, Encoding.UTF8, "application/fhir+json")
        };

        // Add required NEMS headers
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.JwtToken);
        httpRequest.Headers.Add("fromASID", request.FromAsid);
        httpRequest.Headers.Add("toASID", request.ToAsid);
        httpRequest.Headers.Add("InteractionID", "urn:nhs:names:services:clinicals-sync:SubscriptionsApiPost");

        var response = await client.SendAsync(httpRequest);

        _logger.LogInformation("NEMS API Response: {StatusCode}", response.StatusCode);

        // Log response for debugging
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("NEMS API Error Response: {Response}", responseContent);
        }

        return response;

    }

    /// <summary>
    /// Sends a DELETE request to remove a NEMS subscription with proper authentication and headers.
    /// </summary>
    /// <param name="request">NEMS subscription DELETE request object</param>
    /// <param name="timeoutSeconds">HTTP client timeout in seconds. Defaults to 300 seconds (5 minutes)</param>
    /// <returns>HTTP response from NEMS API</returns>
    public async Task<HttpResponseMessage> SendSubscriptionDelete(NemsSubscriptionRequest request, int timeoutSeconds = 300)
    {
        var client = _nemsHttpClientProvider.CreateClient(request.ClientCertificate, request.BypassCertValidation);
        client.BaseAddress = new Uri(request.Url);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, request.Url);

        // Add required NEMS headers for delete
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.JwtToken);
        httpRequest.Headers.Add("fromASID", request.FromAsid);
        httpRequest.Headers.Add("toASID", request.ToAsid);
        httpRequest.Headers.Add("InteractionID", "urn:nhs:names:services:clinicals-sync:SubscriptionsApiDelete");

        var response = await client.SendAsync(httpRequest);

        _logger.LogInformation("NEMS API Response: {StatusCode}", response.StatusCode);
        // Log response for debugging
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("NEMS API Error Response: {Response}", responseContent);
        }
        return response;
    }

    /// <summary>
    /// Generates an unsigned JWT token for NEMS API authentication.
    /// </summary>
    /// <param name="asid">The ASID (Application Service Identifier) for the requesting system</param>
    /// <param name="audience">The target audience for the token</param>
    /// <param name="scope">The requested scope for the token</param>
    /// <returns>Base64-encoded JWT token without signature</returns>
    public string GenerateJwtToken(string asid, string audience, string scope)
    {
        var header = new
        {
            alg = "none",
            typ = "JWT"
        };

        var payload = new
        {
            iss = "https://nems.nhs.uk",
            sub = $"https://fhir.nhs.uk/Id/accredited-system|{asid}",
            aud = audience,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            reason_for_request = "directcare",
            scope = scope,
            requesting_system = $"https://fhir.nhs.uk/Id/accredited-system|{asid}"
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Unsigned JWT (signature is empty)
        return $"{headerEncoded}.{payloadEncoded}.";
    }
}
