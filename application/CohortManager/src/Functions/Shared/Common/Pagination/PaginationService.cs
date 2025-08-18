namespace Common;

using Microsoft.Azure.Functions.Worker.Http;

public class PaginationService<T> : IPaginationService<T>
{
    private const int pageSize = 10;

    public PaginationResult<T> GetPaginatedResult(IQueryable<T> source, int? lastId, Func<T, int>? idSelector = null)
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

    public Dictionary<string, string> BuildPaginationHeaders<TEntity>(HttpRequestData request, PaginationResult<TEntity> paginationResult)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Total-Count"] = paginationResult.TotalItems.ToString(),
            ["X-Has-Next-Page"] = paginationResult.HasNextPage.ToString().ToLower(),
            ["X-Is-First-Page"] = paginationResult.IsFirstPage.ToString().ToLower()
        };

        if (paginationResult.LastResultId.HasValue)
        {
            headers["X-Last-Id"] = paginationResult.LastResultId.Value.ToString();
        }

        var linkHeaders = BuildLinkHeaders(request, paginationResult);
        if (linkHeaders.Count > 0)
        {
            headers["Link"] = string.Join(", ", linkHeaders);
        }

        return headers;
    }

    private static List<string> BuildLinkHeaders<TEntity>(HttpRequestData request, PaginationResult<TEntity> paginationResult)
    {
        var linkHeaders = new List<string>();
        var baseUrl = request.Url.GetLeftPart(UriPartial.Path);
        var queryString = request.Url.Query;
        var baseQuery = RemoveLastIdParam(queryString);
        var separator = string.IsNullOrEmpty(baseQuery) ? "?" : "&";

        // First page link (no lastId)
        linkHeaders.Add($"<{baseUrl}{baseQuery}>; rel=\"first\"");

        // Next page link (only if has next page)
        if (paginationResult.HasNextPage && paginationResult.LastResultId.HasValue)
        {
            linkHeaders.Add($"<{baseUrl}{baseQuery}{separator}lastId={paginationResult.LastResultId.Value}>; rel=\"next\"");
        }

        return linkHeaders;
    }

    private static string RemoveLastIdParam(string queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return "";

        var pairs = queryString.TrimStart('?').Split('&')
            .Where(p => !p.StartsWith("lastId="))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return pairs.Length > 0 ? "?" + string.Join("&", pairs) : "";
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
