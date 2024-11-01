namespace NHS.CohortManager.CohortDistribution;

using Model;

public class CreateCohortDistributionRequestBody
{
    public string? NhsNumber { get; set; }
    public string? ScreeningService { get; set; }
    public string? FileName { get; set; }
    public string? RecordType { get; set; }
    public Participant? ErrorRecord { get; set; }
}
