namespace Model.DTO;

using Model.Enums;
using Model.Pagination;

/// <summary>
/// Response model for exception search containing paginated exceptions and associated reports
/// </summary>
public class ValidationExceptionsResponse
{
    public SearchType SearchType { get; set; }
    public string SearchValue { get; set; } = string.Empty;
    public List<ValidationException> Exceptions { get; set; } = [];
    public List<ValidationExceptionReport> Reports { get; set; } = [];
    public PaginationResult<ValidationException> PaginatedExceptions { get; set; } = new() { Items = [] };
}

public class ValidationExceptionReport
{
    public DateTime ReportDate { get; set; }
    public int? Category { get; set; }
    public int ExceptionCount { get; set; }
}
