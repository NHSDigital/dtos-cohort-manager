namespace Model.DTO;

using Model.Pagination;

/// <summary>
/// Response model for NHS number search containing paginated exceptions and associated reports
/// </summary>
public class ValidationExceptionsByNhsNumberResponse
{
    public string NhsNumber { get; set; } = string.Empty;
    public PaginationResult<ValidationException> Exceptions { get; set; } = new();
    public List<ValidationExceptionReport> Reports { get; set; } = new();
}

public class ValidationExceptionReport
{
    public DateTime ReportDate { get; set; }
    public string? FileName { get; set; }
    public string? ScreeningName { get; set; }
    public string? CohortName { get; set; }
    public int? Category { get; set; }
    public int ExceptionCount { get; set; }
}
