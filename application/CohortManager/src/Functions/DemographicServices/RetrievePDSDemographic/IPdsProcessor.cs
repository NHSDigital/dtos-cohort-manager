namespace NHS.CohortManager.DemographicServices;

using Model;

public interface IPdsProcessor
{
    Task ProcessPdsNotFoundResponse(HttpResponseMessage pdsResponse, string nhsNumber);
    Task ProcessRecord(Participant participant);
    Task<bool> UpsertDemographicRecordFromPDS(ParticipantDemographic participantDemographic);
}