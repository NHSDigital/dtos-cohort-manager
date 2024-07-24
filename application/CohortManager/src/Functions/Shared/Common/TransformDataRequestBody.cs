namespace NHS.CohortManager.CohortDistribution;

using Model;

public class TransformDataRequestBody
{
    public CohortDistributionParticipant Participant { get; set; }
    public string? ServiceProvider { get; set; }
}
