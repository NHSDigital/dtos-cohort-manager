namespace Common;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class HttpClientFunction : IHttpClientFunction
{
    private readonly ILogger<HttpClientFunction> _logger;
    private readonly IHttpClientFactory _factory;
    public static readonly TimeSpan _timeout = TimeSpan.FromSeconds(300);
    private const string errorMessage = "Failed to execute request to {Url}, message: {Message}";

    public HttpClientFunction(ILogger<HttpClientFunction> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public async Task<HttpResponseMessage> SendPost(string url, string data)
    {
        using var client = _factory.CreateClient();
        using StringContent jsonContent = new(data, Encoding.UTF8, "application/json");

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        try
        {
            HttpResponseMessage response = await client.PostAsync(url, jsonContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, url, ex.Message);
            throw;
        }
    }

    public async Task<string> SendGet(string url)
    {
        using var client = _factory.CreateClient();

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        return await GetAsync(client);
    }

    public async Task<string> SendGet(string url, Dictionary<string, string> parameters)
    {
        using var client = _factory.CreateClient();

        url = QueryHelpers.AddQueryString(url, parameters);

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        return await GetAsync(client);
    }

    public async Task<HttpResponseMessage> SendPdsGet(string url)
    {
        using var client = _factory.CreateClient();

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;
        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, RemoveURLQueryString(url), ex.Message);
            throw;
        }
    }

    public async Task<HttpResponseMessage> SendNemsPost(
        string url,
        string subscriptionJson,
        string jwtToken,
        string fromAsid,
        string toAsid,
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        var handler = ConfigureNemsHttpClientHandler(clientCertificate, bypassCertValidation);


        using var client = new HttpClient(handler);
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

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute NEMS POST request to {Url}: {ErrorMessage}", RemoveURLQueryString(url), ex.Message);
            throw;
        }
    }

    public async Task<HttpResponseMessage> SendNemsDelete(
        string url,
        string jwtToken,
        string fromAsid,
        string toAsid,
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        var handler = ConfigureNemsHttpClientHandler(clientCertificate, bypassCertValidation);

        using var client = new HttpClient(handler);
        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        // Add required NEMS headers for delete
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        request.Headers.Add("fromASID", fromAsid);
        request.Headers.Add("toASID", toAsid);
        request.Headers.Add("InteractionID", "urn:nhs:names:services:clinicals-sync:SubscriptionsApiDelete");

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, RemoveURLQueryString(url), ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Generates a JWT token for NEMS API authentication
    /// </summary>
    /// <param name="asid">Your ASID</param>
    /// <param name="audience">The NEMS endpoint</param>
    /// <param name="scope">The required scope (e.g., patient/Subscription.write)</param>
    /// <returns>Unsigned JWT token</returns>
    public string GenerateNemsJwtToken(string asid, string audience, string scope)
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

    public async Task<HttpResponseMessage> SendPut(string url, string data)
    {
        using var client = _factory.CreateClient();
        using StringContent jsonContent = new(data, Encoding.UTF8, "application/json");

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        try
        {
            HttpResponseMessage response = await client.PutAsync(url, jsonContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, url, ex.Message);
            throw;
        }
    }

    public async Task<bool> SendDelete(string url)
    {
        using var client = _factory.CreateClient();

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        try
        {
            HttpResponseMessage response = await client.DeleteAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, url, ex.Message);
            throw;
        }
    }

    public async Task<string> GetResponseText(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Removes the query string from the URL to prevent us logging sensitive information.
    /// </summary>
    private static string RemoveURLQueryString(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        int queryIndex = url.IndexOf('?');
        return queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
    }

    /// <summary>
    /// Reads response content from successful GET requests and returns it as a string. Returns null for unsuccessful requests.
    /// </summary>
    private async Task<string> GetAsync(HttpClient client)
    {
        var url = client.BaseAddress?.ToString() ?? string.Empty;

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await GetResponseText(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, RemoveURLQueryString(url), ex.Message);
            throw;
        }

        return string.Empty;
    }

    private HttpClientHandler ConfigureNemsHttpClientHandler(
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        var handler = new HttpClientHandler();

        // Add client certificate for mutual TLS authentication
        if (clientCertificate != null)
        {
            handler.ClientCertificates.Add(clientCertificate);
            _logger.LogInformation("Added client certificate for NEMS authentication");
        }

#if DEBUG
        if (bypassCertValidation)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                _logger.LogWarning("Bypassing server certificate validation - DO NOT USE IN PRODUCTION");
                
                // Still perform basic certificate validation even when bypassing
                if (cert == null)
                {
                    _logger.LogError("Server certificate is null");
                    return false;
                }
                
                // Check if certificate is expired
                if (cert.NotAfter < DateTime.Now || cert.NotBefore > DateTime.Now)
                {
                    _logger.LogError("Server certificate is expired or not yet valid");
                    return false;
                }
                
                return true;
            };
        }
#else
// In Release/Production: Never bypass, always validate (default behavior)
#endif

        return handler;
    }
}
