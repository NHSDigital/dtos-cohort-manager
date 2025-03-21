namespace Common.Interfaces;

using Model;
using Model.DTO;

public interface ICreateCohortDistributionData
{
    List<CohortDistributionParticipantDto> GetUnextractedCohortDistributionParticipants(int rowCount);
    bool UpdateCohortParticipantAsInactive(string NhsNumber);
    CohortDistributionParticipant GetLastCohortDistributionParticipant(string NhsNumber);
    Task<List<CohortDistributionParticipantDto>> GetCohortDistributionParticipantsByRequestId(string requestId);
    Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom);
    CohortRequestAudit GetNextCohortRequestAudit(string requestId);
}
