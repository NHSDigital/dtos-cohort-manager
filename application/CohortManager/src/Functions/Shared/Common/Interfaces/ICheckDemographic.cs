namespace Common;

using Model;

public interface ICheckDemographic
{
    Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI);
    Task<bool> PostDemographicBatchAsync(List<Participant> participantList, string DemographicFunctionURI);
}
