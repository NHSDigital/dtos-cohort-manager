namespace Data.Database;

using Model;

public interface IUpdateParticipantData
{
    public bool UpdateParticipantAsEligible(Participant participant, char isActive);
    public Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord);
    public Participant GetParticipant(string NhsNumber);
}
