namespace Common;

public interface IPaginationService<T>
{
    PaginationResult<T> GetPaginatedResult(
        IQueryable<T> source,
        int? lastId,
        int pageSize = 20,
        Func<T, int> idSelector = null);
}
