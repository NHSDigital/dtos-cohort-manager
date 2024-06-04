using Model;

namespace Common;

public interface ICheckDemographic
{
    Task<Demographic> CheckDemographicAsync(string NhsId, string DemographicFunctionURI);
}