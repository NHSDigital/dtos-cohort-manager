namespace Common;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface ICohortDistributionHelper
{
    Task<CohortDistributionParticipant> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody requestBody);
    Task<string> AllocateServiceProviderAsync(CreateCohortDistributionRequestBody requestBody, string postCode);
    Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData);
}
