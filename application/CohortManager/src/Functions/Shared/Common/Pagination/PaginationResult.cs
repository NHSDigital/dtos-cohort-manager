namespace Common;

public class PaginationResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public bool HasMoreItems { get; set; }
    public int? NextLastId { get; set; }
}
