namespace addParticipant;

using Model;

public interface IAddParticipantProcessor
{
    Task AddParticipant(BasicParticipantCsvRecord participantCsvRecord);
}
