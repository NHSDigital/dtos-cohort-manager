namespace Model.DTO;

using Model.Pagination;

/// <summary>
/// Response model for NHS number search containing paginated exceptions and associated reports
/// </summary>
public class ValidationExceptionsByNhsNumberResponse
{
    public string NhsNumber { get; set; } = string.Empty;
    public PaginationResult<ValidationException> Exceptions { get; set; } = new() { Items = [] };
    public List<ValidationExceptionReport> Reports { get; set; } = [];
}

public class ValidationExceptionReport
{
    public DateTime ReportDate { get; set; }
    public int? Category { get; set; }
    public int ExceptionCount { get; set; }
}
