namespace Model;

public class ValidationException
{
    public string? FileName { get; set; }
    public string? NhsNumber { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateResolved { get; set; }
    public int? RuleId { get; set; }
    public string? RuleDescription { get; set; }
    public string? RuleContent { get; set; }
    public int? Category { get; set; }
    public int? ScreeningService { get; set; }
    public string? Cohort { get; set; }
    public int? Fatal { get; set; }
}
