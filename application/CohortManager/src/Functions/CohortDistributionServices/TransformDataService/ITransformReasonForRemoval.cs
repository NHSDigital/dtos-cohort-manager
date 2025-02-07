namespace NHS.CohortManager.CohortDistribution;

using Model;
public interface ITransformReasonForRemoval
{
    Task<CohortDistributionParticipant> ReasonForRemovalTransformations(CohortDistributionParticipant participant, CohortDistribution? existingParticipant);
}
