namespace Common;

using Model;

public interface ICheckDemographic
{
    Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI);
    Task<bool> PostDemographicDataAsync(List<Participant> participants, string DemographicFunctionURI);
}
