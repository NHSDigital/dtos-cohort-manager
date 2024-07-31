namespace Data.Database;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface IParticipantManagerData
{
    public bool UpdateParticipantAsEligible(Participant participant, char isActive);
    public Task<bool> UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord);
    public Participant GetParticipant(string NhsNumber);
    Participant GetParticipantFromIDAndScreeningService(RetrieveParticipantRequestBody retrieveParticipantRequestBody);
}
