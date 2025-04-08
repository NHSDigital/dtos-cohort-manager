namespace Common;

using System.Text;
using Microsoft.Extensions.Logging;

public class HttpClientFunction : IHttpClientFunction
{
    private readonly ILogger<HttpClientFunction> _logger;
    private readonly IHttpClientFactory _factory;
    public static readonly TimeSpan _timeout = TimeSpan.FromMinutes(10);

    public HttpClientFunction(ILogger<HttpClientFunction> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers)
    {
        using var client = _factory.CreateClient();

        client.BaseAddress = new Uri(url);
        client.Timeout = _timeout;

        if (headers != null)
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute request to {Url}, message: {Message}", RemoveURLQueryString(url), ex.Message);
            throw;
        }
    }

    public async Task<HttpResponseMessage> PostAsync(string url, string data)
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
            _logger.LogError(ex, "Failed to execute request to {Url}, message: {Message}", url, ex.Message);
            throw;
        }
    }

    public async Task<HttpResponseMessage> PutAsync(string url, string data)
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
            _logger.LogError(ex, "Failed to execute request to {Url}, message: {Message}", url, ex.Message);
            throw;
        }
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
}
