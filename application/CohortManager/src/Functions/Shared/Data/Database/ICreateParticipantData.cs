namespace Data.Database;
using Model;

public interface ICreateParticipantData
{
    public bool CreateParticipantEntryAsync(Participant participantData, string cString);
}
