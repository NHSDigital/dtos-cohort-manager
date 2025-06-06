namespace NHS.CohortManager.CohortDistributionService;

using Model;
using Common;

public interface ICohortDistributionHelper
{
    Task<CohortDistributionParticipant?> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody);
    Task<string> AllocateServiceProviderAsync(string nhsNumber, string screeningAcronym, string postCode, string errorRecord);
    Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData, CohortDistributionParticipant existingParticipant);
    Task<ValidationExceptionLog> ValidateCohortDistributionRecordAsync(string fileName, CohortDistributionParticipant requestParticipant, CohortDistributionParticipant existingParticipant);
}
