namespace NHS.CohortManager.CohortDistribution;

using Model;

public class TransformDataRequestBody
{
    public CohortDistributionParticipant Participant { get; set; }
    public string? ScreeningService { get; set; }

    public TransformDataRequestBody(CohortDistributionParticipant participant, string screeningService)
    {
        Participant = participant;
        ScreeningService = screeningService;
    }
}
