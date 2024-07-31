namespace Common;

using Model;
using NHS.CohortManager.CohortDistribution;

public interface ICohortDistributionHelper
{
    public Task<CohortDistributionParticipant> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody requestBody);
    public Task<string> AllocateServiceProviderAsync(CreateCohortDistributionRequestBody requestBody, CohortDistributionParticipant participantData);
    public Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData);
}
