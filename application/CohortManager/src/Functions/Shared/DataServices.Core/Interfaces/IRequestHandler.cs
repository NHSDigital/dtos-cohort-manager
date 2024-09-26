public interface IRequestHandler<TEntity>
{
    Task<DataServiceResponse<string>> HandleRequest(HttpRequestMessage httpRequestMessage, Func<TEntity,bool> keyPredicate);
}
