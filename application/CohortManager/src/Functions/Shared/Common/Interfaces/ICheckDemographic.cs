namespace Common;

using Model;

public interface ICheckDemographic
{
    Task<Demographic> GetDemographicAsync(string NhsId, string DemographicFunctionURI);
    Task<bool> PostDemographicDataAsync(Participant participant, string DemographicFunctionURI);
}
