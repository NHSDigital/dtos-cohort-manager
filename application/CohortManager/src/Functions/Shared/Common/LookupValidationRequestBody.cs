namespace Common;

using Model;

public class LookupValidationRequestBody
{
    public Participant ExistingParticipant { get; set; }
    public Participant NewParticipant { get; set; }
    public string FileName { get; set; }

    public LookupValidationRequestBody(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        ExistingParticipant = existingParticipant;
        NewParticipant = newParticipant;
        FileName = fileName;
    }
}
