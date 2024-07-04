namespace NHS.CohortManager.CohortDistribution;

using Model;

public class TransformDataRequestBody
{
    public Participant Participant { get; set; }
    public string? ScreeningService { get; set; }

    public TransformDataRequestBody(Participant participant, string screeningService)
    {
        Participant = participant;
        ScreeningService = screeningService;
    }
}
