namespace NHS.CohortManager.CohortDistribution;

using Model;

public interface ICohortDistributionHelper
{
    Task<CohortDistributionParticipant?> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody);
    Task<string> AllocateServiceProviderAsync(string nhsNumber, string screeningAcronym, string postCode, string errorRecord);
    Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData);
    Task<ValidationExceptionLog> ValidateCohortDistributionRecordAsync(string fileName, CohortDistributionParticipant requestParticipant, CohortDistributionParticipant existingParticipant);
}
