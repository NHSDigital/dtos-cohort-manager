namespace Common;

public interface IPaginationService<T>
{
    PaginationResult<T> GetPaginatedResult(
        IQueryable<T> source,
        int? lastId,
        Func<T, int> idSelector = null);
}
