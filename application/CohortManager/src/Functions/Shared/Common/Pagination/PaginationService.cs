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
        idSelector ??= GetDefaultIdSelector();

        // If lastId is not provided, start from the beginning
        IQueryable<T> query = lastId.HasValue && lastId.Value > 0
            ? source.Where(x => idSelector(x) > lastId.Value)
            : source;

        // Order the query by ID
        query = query.OrderBy(x => idSelector(x));

        // Take one extra item to check if there are more items
        var items = query.Take(pageSize + 1).ToList();

        // Determine if there are more items
        bool hasMoreItems = items.Count > pageSize;

        // Remove the extra item if present
        var resultItems = hasMoreItems ? items.Take(pageSize).ToList() : items;

        // Get the last ID for the next page
        int? nextLastId = hasMoreItems
            ? idSelector(resultItems.Last())
            : null;

        return new PaginationResult<T>
        {
            Items = resultItems,
            HasMoreItems = hasMoreItems,
            NextLastId = nextLastId
        };
    }

    private static Func<T, int> GetDefaultIdSelector()
    {
        // Try to get the Id property dynamically
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

