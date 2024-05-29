namespace NHS.CohortManager.ValidationDataService;

using Model;

public class ValidationFunctionRequestBody
{
    public string Workflow { get; set; }
    public Participant ExistingParticipant { get; set; }
    public Participant NewParticipant { get; set; }

    public ValidationFunctionRequestBody(string workflow, Participant existingParticipant, Participant newParticipant)
    {
        Workflow = workflow;
        ExistingParticipant = existingParticipant;
        NewParticipant = newParticipant;
    }
}
