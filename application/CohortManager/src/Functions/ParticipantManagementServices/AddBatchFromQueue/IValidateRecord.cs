using Model;

public interface IValidateRecord
{
    Task<(Participant Participant, ValidationExceptionLog ValidationExceptionLog)> ValidateData(ParticipantCsvRecord participantCsvRecord, Participant participant);
    Task<ParticipantCsvRecord> ValidateLookUpData(ParticipantCsvRecord newParticipantCsvRecord, string fileName);
}