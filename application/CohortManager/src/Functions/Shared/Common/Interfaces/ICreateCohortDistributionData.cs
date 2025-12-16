namespace Common.Interfaces;

using Model;
using Model.DTO;

public interface ICreateCohortDistributionData
{
    Task<List<CohortDistributionParticipantDto>> GetUnextractedCohortDistributionParticipants(int rowCount, bool retrieveSupersededRecordsLast);
    Task<List<CohortDistributionParticipantDto>> GetCohortDistributionParticipantsByRequestId(Guid requestId);
    Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom);
    Task<CohortRequestAudit> GetNextCohortRequestAudit(Guid requestId);


}
