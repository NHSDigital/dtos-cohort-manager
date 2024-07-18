namespace Common.Interfaces;

using Model;


public interface ICreateCohortDistributionData
{
    public bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipantParticipant);
    public List<CohortDistributionParticipant> ExtractCohortDistributionParticipants();
    public bool UpdateCohortParticipantAsInactive(string NhsNumber);
}
