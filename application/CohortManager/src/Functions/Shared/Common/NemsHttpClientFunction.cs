namespace Common;

using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class NemsHttpClientFunction : HttpClientFunction, INemsHttpClientFunction
{
    private readonly INemsHttpClientProvider _nemsHttpClientProvider;
    public new static readonly TimeSpan _timeout = TimeSpan.FromSeconds(300);

    public NemsHttpClientFunction(
        ILogger<NemsHttpClientFunction> logger,
        IHttpClientFactory factory,
        INemsHttpClientProvider nemsHttpClientProvider)
        : base(logger, factory)
    {
        _nemsHttpClientProvider = nemsHttpClientProvider;
    }

    public async Task<HttpResponseMessage> SendSubscriptionPost(
        string url,
        string subscriptionJson,
        string jwtToken,
        string fromAsid,
        string toAsid,
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        using var client = _nemsHttpClientProvider.CreateClient(clientCertificate, bypassCertValidation);
        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(subscriptionJson, Encoding.UTF8, "application/fhir+json")
        };

        // Add required NEMS headers
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        request.Headers.Add("fromASID", fromAsid);
        request.Headers.Add("toASID", toAsid);
        request.Headers.Add("InteractionID", "urn:nhs:names:services:clinicals-sync:SubscriptionsApiPost");

        var response = await client.SendAsync(request);

        _logger.LogInformation("NEMS API Response: {StatusCode}", response.StatusCode);

        // Log response for debugging
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("NEMS API Error Response: {Response}", responseContent);
        }

        return response;

    }

    public async Task<HttpResponseMessage> SendSubscriptionDelete(
        string url,
        string jwtToken,
        string fromAsid,
        string toAsid,
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        using var client = _nemsHttpClientProvider.CreateClient(clientCertificate, bypassCertValidation);
        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        // Add required NEMS headers for delete
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        request.Headers.Add("fromASID", fromAsid);
        request.Headers.Add("toASID", toAsid);
        request.Headers.Add("InteractionID", "urn:nhs:names:services:clinicals-sync:SubscriptionsApiDelete");

        var response = await client.SendAsync(request);

        _logger.LogInformation("NEMS API Response: {StatusCode}", response.StatusCode);
        // Log response for debugging
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("NEMS API Error Response: {Response}", responseContent);
        }
        return response;
    }

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

    /// <summary>
    /// Removes the query string from the URL to prevent us logging sensitive information.
    /// </summary>
    private new static string RemoveURLQueryString(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        int queryIndex = url.IndexOf('?');
        return queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
    }
}
