namespace Common;

public class PaginationService<T> : IPaginationService<T>
{
    public PaginationResult<T> GetPaginatedResult(
        IQueryable<T> source,
        int? lastId,
        int pageSize = 20,
        Func<T, int> idSelector = null)
    {
        // If no idSelector is provided, try to use a default 'Id' property
        if (idSelector == null)
        {
            idSelector = GetDefaultIdSelector();
        }

        var IsFirstPage = !lastId.HasValue || lastId.Value == 0;

        var query = lastId.HasValue && lastId.Value > 0
            ? source.Where(s => idSelector(s) > lastId.Value)
            : source;

        query = query.OrderBy(x => idSelector(x));

        var resultItems = query.Take(pageSize + 1).ToList();
        bool hasMoreItems = resultItems.Count > pageSize;
        int? lastResultId = hasMoreItems ? idSelector(resultItems[resultItems.Count - 1]) : null;
        var totalItems = source.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        int currentPage = !IsFirstPage ? (int)Math.Ceiling((double)(lastId.Value + resultItems.Count) / pageSize) : 1;

        return new PaginationResult<T>
        {
            Items = resultItems,
            IsFirstPage = IsFirstPage,
            HasNextPage = hasMoreItems,
            LastResultId = lastResultId,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
    }

    private static Func<T, int> GetDefaultIdSelector()
    {
        var idProperty = typeof(T).GetProperty("Id") ??
            typeof(T).GetProperty($"{typeof(T).Name}Id");

        if (idProperty == null)
        {
            throw new InvalidOperationException(
                "Could not find a default ID property. Provide a custom ID selector.");
        }
        return x => (int)idProperty.GetValue(x);
    }
}

