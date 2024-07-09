namespace Common;

using Model;

public interface IHandleException
{
    Task<Participant> CheckStaticValidationRules(ParticipantCsvRecord participantCsvRecord);

    Task<Participant> CheckLookupValidationRules(Participant existingParticipant, Participant newParticipant, string fileName, string workFlow);

    Task<bool> DemographicDataRetrievedSuccessfully(Demographic demographicData, Participant participant, string fileName);

    Task<bool> CallExceptionFunction(Participant participant, string fileName);
}
