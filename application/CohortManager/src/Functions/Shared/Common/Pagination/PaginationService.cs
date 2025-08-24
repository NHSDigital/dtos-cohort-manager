namespace Common;

public class PaginationService<T> : IPaginationService<T>
{
    private const int pageSize = 10;

    public PaginationResult<T> GetPaginatedResult(IQueryable<T> source, int? lastId, Func<T, int> idSelector = null)
    {
        // If no idSelector is provided, try to use a default 'Id' property
        if (idSelector == null)
        {
            idSelector = GetDefaultIdSelector();
        }

        // Convert source to a list of IDs to calculate index-based pagination
        var idList = source.Select(idSelector).OrderBy(id => id).ToList();

        // Get the index of the lastId
        int lastIdIndex = lastId.HasValue ? idList.IndexOf(lastId.Value) : -1;
        int currentPage = lastIdIndex >= 0 ? (lastIdIndex / pageSize) + 2 : 1;
        var totalItems = source.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        if (lastIdIndex >= 0)
        {
            source = source.Skip(lastIdIndex + 1);
        }

        var items = source.Take(pageSize).ToList();
        int? lastResultId = items.Count > 0 ? idSelector(items[items.Count - 1]) : null;

        return new PaginationResult<T>
        {
            Items = items,
            IsFirstPage = currentPage == 1,
            HasNextPage = lastResultId.HasValue && (lastIdIndex + pageSize < totalItems),
            LastResultId = lastResultId,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
    }

    private static Func<T, int> GetDefaultIdSelector()
    {
        var idProperty = (typeof(T).GetProperty("Id") ??
            typeof(T).GetProperty($"{typeof(T).Name}Id")) ?? throw new InvalidOperationException(
                "Could not find a default ID property. Provide a custom ID selector.");
        return x => (int)idProperty.GetValue(x);
    }
}

