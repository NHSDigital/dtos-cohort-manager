
namespace NHS.CohortManager.DemographicServices;

public interface IBearerTokenService
{
    Task<string> GetBearerToken();
}