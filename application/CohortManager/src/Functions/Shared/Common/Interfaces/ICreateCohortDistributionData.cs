namespace Common.Interfaces;

using Model;


public interface ICreateCohortDistributionData
{
    bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant);
    List<CohortDistributionParticipant> ExtractCohortDistributionParticipants(int screeningServiceId, int rowCount);
    bool UpdateCohortParticipantAsInactive(string NhsNumber);
    CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber);
    List<CohortDistributionParticipant> GetCohortDistributionParticipantsMock(int serviceProviderId, int rowCount, string testDataJson);
    List<CohortDistributionParticipant> GetCohortDistributionParticipantsByRequestId(string requestId);
    List<CohortAuditHistory> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom);
}
