namespace Common;

using Microsoft.Azure.Functions.Worker.Http;
using Model.Pagination;

public class PaginationService<T> : IPaginationService<T>
{
    /// <summary>
    /// Gets paginated results using page-based pagination
    /// </summary>
    /// <param name="source">The queryable source</param>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page (default: 10)</param>
    /// <returns>Paginated result</returns>
    public PaginationResult<T> GetPaginatedResult(IQueryable<T> source, int page, int pageSize)
    {
        if (pageSize <= 0) pageSize = 10;
        if (page <= 0) page = 1;

        var totalItems = source.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        // Ensure page is within valid range
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var items = source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationResult<T>
        {
            Items = items,
            IsFirstPage = page == 1,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = page,
        };
    }

    /// <summary>
    /// Adds pagination navigation headers to the response.
    /// </summary>
    public Dictionary<string, string> AddNavigationHeaders<TEntity>(HttpRequestData request, PaginationResult<TEntity> paginationResult)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Total-Count"] = paginationResult.TotalItems.ToString(),
            ["X-Has-Next-Page"] = paginationResult.HasNextPage.ToString().ToLower(),
            ["X-Has-Previous-Page"] = paginationResult.HasPreviousPage.ToString().ToLower(),
            ["X-Is-First-Page"] = paginationResult.IsFirstPage.ToString().ToLower(),
            ["X-Current-Page"] = paginationResult.CurrentPage.ToString(),
            ["X-Total-Pages"] = paginationResult.TotalPages.ToString()
        };

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
        var baseQuery = RemovePageParam(queryString);
        var separator = string.IsNullOrEmpty(baseQuery) ? "?" : "&";

        // First page link
        linkHeaders.Add($"<{baseUrl}{baseQuery}>; rel=\"first\"");

        // Previous page link
        if (paginationResult.HasPreviousPage)
        {
            var prevPage = paginationResult.CurrentPage - 1;
            var prevUrl = prevPage == 1
                ? $"{baseUrl}{baseQuery}"
                : $"{baseUrl}{baseQuery}{separator}page={prevPage}";
            linkHeaders.Add($"<{prevUrl}>; rel=\"prev\"");
        }

        // Next page link
        if (paginationResult.HasNextPage)
        {
            var nextPage = paginationResult.CurrentPage + 1;
            linkHeaders.Add($"<{baseUrl}{baseQuery}{separator}page={nextPage}>; rel=\"next\"");
        }

        // Last page link
        if (paginationResult.TotalPages > 1)
        {
            linkHeaders.Add($"<{baseUrl}{baseQuery}{separator}page={paginationResult.TotalPages}>; rel=\"last\"");
        }

        return linkHeaders;
    }

    private static string RemovePageParam(string queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return "";

        var pairs = queryString.TrimStart('?').Split('&')
            .Where(p => !p.StartsWith("page="))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return pairs.Length > 0 ? "?" + string.Join("&", pairs) : "";
    }
}
