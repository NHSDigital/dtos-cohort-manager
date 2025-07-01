namespace NHS.CohortManager.CohortDistributionService;

using Model;
public interface IUnTransformRules
{
    Task<CohortDistributionParticipant> TooManyDemographicsFieldsChanges(CohortDistributionParticipant participant, CohortDistribution? existingParticipant);
}
