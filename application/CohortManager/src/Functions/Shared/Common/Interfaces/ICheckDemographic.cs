namespace Common;

using Model;

public interface ICheckDemographic
{
    Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI);
    Task<bool> PostDemographicDataAsync(List<ParticipantDemographic> participants, string DemographicFunctionURI);
}
