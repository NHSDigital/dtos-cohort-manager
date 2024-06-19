namespace Model;

public class ValidationException
{
    public string? RuleId { get; set; }
    public string? RuleName { get; set; }
    public string? Workflow { get; set; }
    public string? NhsNumber { get; set; }
    public DateTime? DateCreated { get; set; }
    public string? FileName { get; set; }
}
