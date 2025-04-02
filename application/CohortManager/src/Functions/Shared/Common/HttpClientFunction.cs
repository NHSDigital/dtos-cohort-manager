namespace Common;

using Microsoft.Extensions.Logging;

public class HttpClientFunction : IHttpClientFunction
{
    private readonly ILogger<HttpClientFunction> _logger;
    private readonly IHttpClientFactory _factory;

    public HttpClientFunction(ILogger<HttpClientFunction> logger, IHttpClientFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    /// <summary>
    /// Performs a GET request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="headers">Headers to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers)
    {
        using var client = _factory.CreateClient();

        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromMinutes(1000);

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
