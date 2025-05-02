namespace Common;

using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

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

    public async Task<HttpResponseMessage> SendGetAsync(string url, Dictionary<string, string> parameters)
    {
        using var client = _factory.CreateClient();

        url = QueryHelpers.AddQueryString(url, parameters);

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

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

    public async Task<HttpResponseMessage> PostNemsGet(string url, string subscriptionJson, string spineAccessToken, string fromAsid, string toAsid)
    {
        using var client = _factory.CreateClient();
        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(subscriptionJson, Encoding.UTF8, "application/fhir+json")
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", spineAccessToken);
        request.Headers.Add("fromASID", fromAsid);
        request.Headers.Add("toASID", toAsid);
        request.Headers.Add("Interaction-ID", "urn:nhs:names:services:nems:CreateSubscription");

        try
        {
            var response = await client.SendAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorMessage, RemoveURLQueryString(url), ex.Message);
            throw;
        }
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

        return null;
    }
}
