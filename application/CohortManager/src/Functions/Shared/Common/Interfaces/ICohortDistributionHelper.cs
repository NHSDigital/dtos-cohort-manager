namespace Common;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface ICohortDistributionHelper
{
    Task<CohortDistributionParticipant?> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody);
    Task<string> AllocateServiceProviderAsync(string nhsNumber, string screeningAcronym, string postCode, string errorRecord);
    Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData);
    Task<ValidationExceptionLog> ValidateCohortDistributionRecordAsync(string nhsNumber, string FileName, CohortDistributionParticipant cohortDistributionParticipant);

    Task<ValidationExceptionLog> ValidateStaticeData(Participant participant, string fileName);
}
