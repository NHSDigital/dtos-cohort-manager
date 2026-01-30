using Castle.Core.Logging;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComponentTests;

public class HttpAdapter : IHttpClientFunction
{
    private readonly IHttpClientFunction _httpClient;
    public HttpAdapter(IServiceScope scope)
    {
        _httpClient = new HttpClientFunction(scope.ServiceProvider.GetRequiredService<ILogger<HttpClientFunction>>(),scope.ServiceProvider.GetRequiredService<IHttpClientFactory>());

    }

    public Task<string> GetResponseText(HttpResponseMessage response)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendDelete(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGet(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGet(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGetOrThrowAsync(string url)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendGetResponse(string url)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendGetResponse(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendPdsGet(string url, string bearerToken)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendPost(string url, string data)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendPost(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendPut(string url, string data)
    {
        throw new NotImplementedException();
    }
}
