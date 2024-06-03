namespace Common;

public interface ICheckDemographic
{
    public Task<bool> CheckDemographicAsync(string NhsId, string DemographicFunctionURI);
}