namespace Common;

using Model;
using Model.Enums;

public class LookupValidationRequestBody
{
    public Participant ExistingParticipant { get; set; }
    public Participant NewParticipant { get; set; }
    public string FileName { get; set; }

    public RulesType RulesType { get; set; }

    public LookupValidationRequestBody() { }

    public LookupValidationRequestBody(Participant existingParticipant, Participant newParticipant, string fileName, RulesType rulesType)
    {
        ExistingParticipant = existingParticipant;
        NewParticipant = newParticipant;
        FileName = fileName;
        RulesType = rulesType;
    }
}
