namespace DataServices.Core;
using Microsoft.Azure.Functions.Worker.Http;
public interface IRequestHandler<TEntity>
{
    Task<HttpResponseData> HandleRequest(HttpRequestData req, string? key = null);
}
