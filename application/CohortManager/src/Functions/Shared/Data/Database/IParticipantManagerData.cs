namespace Data.Database;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface IParticipantManagerData
{
    bool UpdateParticipantAsEligible(Participant participant, char isActive);
    Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord, Participant oldParticipant);
    Participant GetParticipant(string NhsNumber);
    Participant GetParticipantFromIDAndScreeningService(RetrieveParticipantRequestBody retrieveParticipantRequestBody);
}
