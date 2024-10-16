using Microsoft.Azure.Functions.Worker.Http;
public interface IRequestHandler<TEntity>
{
    Task<HttpResponseData> HandleRequest(HttpRequestData httpRequestMessage, string? key = null);
}
