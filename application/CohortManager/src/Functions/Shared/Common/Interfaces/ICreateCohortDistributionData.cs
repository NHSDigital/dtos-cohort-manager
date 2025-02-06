namespace Common.Interfaces;

using Model;
using Model.DTO;

public interface ICreateCohortDistributionData
{
    Task<bool> InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant);
    List<CohortDistributionParticipantDto> GetUnextractedCohortDistributionParticipants(int rowCount);
    bool UpdateCohortParticipantAsInactive(string NhsNumber);
    CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber);
    List<CohortDistributionParticipantDto> GetCohortDistributionParticipantsByRequestId(string requestId);
    Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom);
    CohortRequestAudit GetNextCohortRequestAudit(string requestId);
}
