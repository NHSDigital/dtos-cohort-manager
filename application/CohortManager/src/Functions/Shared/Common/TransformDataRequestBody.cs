namespace NHS.CohortManager.CohortDistribution;

using Model;

public class TransformDataRequestBody
{
    public CohortDistributionParticipant Participant { get; set; }
    public CohortDistributionParticipant LatestRecordFromCohortDistribution { get; set; }
    public string? ServiceProvider { get; set; }
}
