namespace Common.Interfaces;

using Model;
using Model.DTO;

public interface ICreateCohortDistributionData
{
    bool InsertCohortDistributionData(CohortDistributionParticipant cohortDistributionParticipant);
    List<CohortDistributionParticipantDto> GetUnextractedCohortDistributionParticipantsByScreeningServiceId(int screeningServiceId, int rowCount);
    bool UpdateCohortParticipantAsInactive(string NhsNumber);
    CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber);
    List<CohortDistributionParticipantDto> GetCohortDistributionParticipantsByRequestId(string requestId, int rowCount);
    Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom);
    List<CohortDistributionParticipantDto> GetParticipantsByRequestIds(List<string> requestIdsList, int rowCount);
    List<CohortRequestAudit> GetOutstandingCohortRequestAudits(string lastRequestId);
}
