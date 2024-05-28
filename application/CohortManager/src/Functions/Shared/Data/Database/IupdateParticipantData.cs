namespace Data.Database;

using Model;

public interface IUpdateParticipantData
{
    public bool UpdateParticipantAsEligible(Participant participant, char isActive);
    public Task<bool> UpdateParticipantDetails(Participant participantData);
    public Participant GetParticipant(string NHSId);
}
