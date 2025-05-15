using Model;

public interface IValidateRecord
{
    Task<(ParticipantCsvRecord participantCsvRecord, Participant participant)> ValidateData(ParticipantCsvRecord participantCsvRecord, Participant participant);
}