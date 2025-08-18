namespace Model;

using Model.Enums;

public class ValidationException
{
    public int ExceptionId { get; set; }
    public string? FileName { get; set; }
    public string? NhsNumber { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateResolved { get; set; }
    public int? RuleId { get; set; }
    public string? RuleDescription { get; set; }
    public string? ErrorRecord { get; set; }
    public int? Category { get; set; }
    public string? ScreeningName { get; set; }
    public DateTime? ExceptionDate { get; set; }
    public string? CohortName { get; set; }
    public int? Fatal { get; set; }
    public ExceptionDetails ExceptionDetails { get; set; }
    public string? ServiceNowId { get; set; }
    public DateTime? ServiceNowCreatedDate { get; set; }
    public DateTime? RecordUpdatedDate { get; set; }
}
