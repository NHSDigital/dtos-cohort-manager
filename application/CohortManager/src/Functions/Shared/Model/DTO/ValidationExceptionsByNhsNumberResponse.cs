namespace Model.DTO;

/// <summary>
/// Response model for NHS number search containing paginated exceptions and associated reports
/// </summary>
public class ValidationExceptionsByNhsNumberResponse
{
    public string NhsNumber { get; set; } = string.Empty;
    public PaginatedExceptionsResult Exceptions { get; set; } = new();
    public List<ValidationExceptionReport> Reports { get; set; } = new();
}

public class PaginatedExceptionsResult
{
    public List<ValidationException> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class ValidationExceptionReport
{
    public DateTime ReportDate { get; set; }
    public string? FileName { get; set; }
    public string? ScreeningName { get; set; }
    public string? CohortName { get; set; }
    public int ExceptionCount { get; set; }
}
