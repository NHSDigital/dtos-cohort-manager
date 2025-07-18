namespace NHS.CohortManager.CohortDistributionServices;

using Model;

public class ValidationRecord
{
    public required string FileName { get; set; }
    public required CohortDistributionParticipant Participant { get; set; }
    public CohortDistributionParticipant? PreviousParticipantRecord { get; set; }
    public string ServiceProvider { get; set; }
}