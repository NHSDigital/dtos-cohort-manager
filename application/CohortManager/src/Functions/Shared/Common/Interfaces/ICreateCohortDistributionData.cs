namespace Common.Interfaces;

using Model;


public interface ICreateCohortDistributionData
{
    public bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant);
    public List<CohortDistributionParticipant> ExtractCohortDistributionParticipants();
    public bool UpdateCohortParticipantAsInactive(string NhsNumber);
    public List<CohortDistributionParticipant> GetCohortDistributionParticipantsMock(int serviceProviderId, int rowCount);
}
