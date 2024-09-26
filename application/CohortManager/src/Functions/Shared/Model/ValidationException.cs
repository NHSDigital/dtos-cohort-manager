namespace Model;

public class ValidationException
{
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
    public string? Cohort { get; set; }
    public int? Fatal { get; set; }
    public int? ScreeningService { get; set; }
}
