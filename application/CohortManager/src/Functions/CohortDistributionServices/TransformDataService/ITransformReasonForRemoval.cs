namespace NHS.CohortManager.CohortDistributionService;

using Model;
public interface ITransformReasonForRemoval
{
    Task<CohortDistributionParticipant> ReasonForRemovalTransformations(CohortDistributionParticipant participant, CohortDistribution? existingParticipant);
}
