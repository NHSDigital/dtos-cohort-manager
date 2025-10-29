namespace Common;

using Microsoft.Azure.Functions.Worker.Http;

public interface IPaginationService<T>
{
    PaginationResult<T> GetPaginatedResult(IQueryable<T> source, int page, int pageSize);
    Dictionary<string, string> AddNavigationHeaders<TEntity>(HttpRequestData request, PaginationResult<TEntity> paginationResult);
}
