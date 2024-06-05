using Model;

namespace Common;

public interface ICheckDemographic
{
    Task<Demographic> GetDemographicAsync(string NhsId, string DemographicFunctionURI);
    Task<bool> PostDemographicDataAsync(Participant participant, string DemographicFunctionURI);
}