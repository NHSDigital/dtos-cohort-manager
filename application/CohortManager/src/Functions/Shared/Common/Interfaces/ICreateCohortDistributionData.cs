namespace Common.Interfaces;

using Model;


public interface ICreateCohortDistributionData
{
    bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant);
    List<CohortDistributionParticipant> ExtractCohortDistributionParticipants();
    bool UpdateCohortParticipantAsInactive(string NhsNumber);
    CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber);
    List<CohortDistributionParticipant> GetCohortDistributionParticipantsMock(int serviceProviderId, int rowCount, string testDataJson);
}
