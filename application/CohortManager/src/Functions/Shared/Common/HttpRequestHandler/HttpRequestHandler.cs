namespace Common;

using System.Net.Http.Json;
using System.Text;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;

public class HttpRequestHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpRequestHandler> _logger;

    public HttpRequestHandler(IHttpClientFactory httpClientFactory, ILogger<HttpRequestHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> PostString(string clientName, string body, string? requestUri= null)
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        var content = new StringContent(body,Encoding.UTF8,"string");
        httpClient.PostAsync(requestUri,new JsonContent(body));
        await Task.CompletedTask;
        return true;
    }
    public async Task<bool> PostObject<TEntity>(string clientName, TEntity body, string? requestUri= null) where TEntity : class
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        var response = await httpClient.PostAsJsonAsync<TEntity>(requestUri,body);
        return true;

    }


    public async Task<bool> SendPost(string clientName, string body, string? requestUri= null)
    {
        var httpClient = _httpClientFactory.CreateClient(clientName);
        var content =
        httpClient.PostAsync(requestUri,new JsonContent(body));
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> SendPut(string url, string body)
    {

    }





}
