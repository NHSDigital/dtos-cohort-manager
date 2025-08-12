namespace NHS.CohortManager.DemographicServices;

public class PdsErrorResponse
{
    public List<PdsIssue>? issue { get; set; }
    public string? resourceType { get; set; }
}
