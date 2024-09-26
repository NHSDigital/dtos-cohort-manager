using Microsoft.Azure.Functions.Worker.Http;
public interface IRequestHandler<TEntity>
{
    Task<DataServiceResponse<string>> HandleRequest(HttpRequestData httpRequestMessage, Func<TEntity,bool> keyPredicate);
}
