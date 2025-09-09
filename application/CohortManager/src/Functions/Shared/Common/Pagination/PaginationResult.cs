namespace Common;

public class PaginationResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public bool IsFirstPage { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
