namespace NHS.Screening.ProcessNemsUpdate;

public class PdsIssue
{
    public string? code { get; set; }
    public PdsErrorDetails? details { get; set; }
    public string? severity { get; set; }
}