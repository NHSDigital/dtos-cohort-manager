namespace Data.Database;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface IParticipantManagerData
{
    bool UpdateParticipantAsEligible(Participant participant);
    [Obsolete("Replacing With data")]
    bool UpdateParticipantDetails(ParticipantCsvRecord participantCsvRecord);
    Participant GetParticipant(string nhsNumber, string screeningId);
    Participant GetParticipantFromIDAndScreeningService(RetrieveParticipantRequestBody retrieveParticipantRequestBody);
}
