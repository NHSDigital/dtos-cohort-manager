namespace Common.Interfaces;

using Model;


public interface ICreateCohortDistributionData
{
    public bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipantParticipant);
    public List<CohortDistributionParticipant> ExtractCohortDistributionParticipants();
    public bool UpdateCohortParticipantAsInactive(string nhsNumber);
    public CohortDistributionParticipant GetCohortParticipant(string nhsNumber);
    Task<string> AllocateCohortParticipantServiceProvider(CohortDistributionParticipant cohortDistributionParticipant, string screeningService);
    Task<CohortDistributionParticipant> TransformCohortParticipant(CohortDistributionParticipant cohortDistributionParticipant, string serviceProvider);
}
