namespace Model;

public class BasicParticipantData : IParticipant
{
    public string? RecordType { get; set; }
    public string NhsNumber { get; set; }
    public string? ReasonForRemoval { get; set; }
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    public string ScreeningId { get; set; }
    public string? ScreeningName { get; set; }
    public string? EligibilityFlag { get; set; }
    public string? Source { get; set; }
    public bool ReferralFlag { get; set; }

    public BasicParticipantData() { }

    public BasicParticipantData(IParticipant participant)
    {
        RecordType = participant.RecordType;
        NhsNumber = participant.NhsNumber;
        ReasonForRemoval = participant.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate;
        ScreeningId = participant.ScreeningId;
        ScreeningName = participant.ScreeningName;
        EligibilityFlag = participant.EligibilityFlag;
        Source = participant.Source;
        ReferralFlag = participant.ReferralFlag;
    }

    public BasicParticipantData(ServiceNowParticipant serviceNowParticipant, ParticipantManagement? participantManagement)
    {
        Source = serviceNowParticipant.ServiceNowCaseNumber;
        ScreeningId = serviceNowParticipant.ScreeningId.ToString();
        NhsNumber = serviceNowParticipant.NhsNumber.ToString();
        RecordType = participantManagement is null ? Actions.New : Actions.Amended;
        ReferralFlag = true;
    }
}
