namespace Data.Database;

using Model;

public interface ICreateParticipantData
{
    public bool CreateParticipantEntry(ParticipantCsvRecord participantCsvRecord);
}
