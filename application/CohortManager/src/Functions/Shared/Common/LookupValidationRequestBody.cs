namespace NHS.CohortManager.ScreeningValidationService;

using Model;

public class LookupValidationRequestBody
{
    public string Workflow { get; set; }
    public Participant ExistingParticipant { get; set; }
    public Participant NewParticipant { get; set; }
    public string FileName { get; set; }

    public LookupValidationRequestBody(string workflow, Participant existingParticipant, Participant newParticipant)
    {
        Workflow = workflow;
        ExistingParticipant = existingParticipant;
        NewParticipant = newParticipant;
    }
}
