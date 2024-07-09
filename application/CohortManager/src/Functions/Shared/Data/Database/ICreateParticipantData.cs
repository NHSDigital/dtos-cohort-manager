namespace Data.Database;

using Model;

public interface ICreateParticipantData
{
    public Task<bool> CreateParticipantEntry(ParticipantCsvRecord participantCsvRecord);
}
