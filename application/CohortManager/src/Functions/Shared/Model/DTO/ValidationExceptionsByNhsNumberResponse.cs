namespace Model.DTO;

using Model.Pagination;

/// <summary>
/// Response model for NHS number search containing paginated exceptions and associated reports
/// </summary>
public class ValidationExceptionsByNhsNumberResponse
{
    public string NhsNumber { get; set; } = string.Empty;
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
