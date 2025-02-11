namespace DataServices.Core;

using Microsoft.Azure.Functions.Worker.Http;

public interface IGetRequestHandler<TEntity> where TEntity : class
{
    Task<HttpResponseData> GetByIdAsync(HttpRequestData req, string keyValue);
    Task<HttpResponseData> Get(HttpRequestData req);
}
