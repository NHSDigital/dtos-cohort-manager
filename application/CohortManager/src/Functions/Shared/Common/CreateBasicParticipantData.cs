namespace Common;

using Model;

public class CreateBasicParticipantData : ICreateBasicParticipantData
{
    public BasicParticipantData BasicParticipantData(Participant participant)
    {
        return new BasicParticipantData
        {
            RecordType = participant.RecordType,
            NhsNumber = participant.NhsNumber,
            RemovalReason = participant.ReasonForRemoval,
            RemovalEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate,
            ScreeningId = participant.ScreeningId,
            ScreeningName = participant.ScreeningName,
            CurrentPosting = participant.CurrentPosting
        };
    }
}
