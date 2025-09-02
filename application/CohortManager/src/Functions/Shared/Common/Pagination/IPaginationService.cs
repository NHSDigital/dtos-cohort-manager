namespace Common;

using Microsoft.Azure.Functions.Worker.Http;

public interface IPaginationService<T>
{
    PaginationResult<T> GetPaginatedResult(IQueryable<T> source, int? lastId, Func<T, int>? idSelector = null);
    Dictionary<string, string> AddNavigationHeaders<TEntity>(HttpRequestData request, PaginationResult<TEntity> paginationResult);
}
