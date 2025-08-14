namespace NHS.CohortManager.DemographicServices;

using System.Net.Http;
using Model;

public interface IPdsProcessor
{
    Task ProcessPdsNotFoundResponse(HttpResponseMessage pdsResponse, string nhsNumber, string? sourceFileName = null);
    Task ProcessRecord(Participant participant, string? fileName = null);
    Task<bool> UpsertDemographicRecordFromPDS(ParticipantDemographic participantDemographic);
}
