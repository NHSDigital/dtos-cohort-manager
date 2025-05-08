namespace Common;

using System.Text.Json;
using Model;

public class CheckDemographic : ICheckDemographic
{
    private readonly IHttpClientFunction _httpClientFunction;

    public CheckDemographic(IHttpClientFunction httpClientFunction)
    {
        _httpClientFunction = httpClientFunction;
    }

    /// <summary>
    /// Retrieves demographic information for a specific NHS number.
    /// </summary>
    /// <param name="NhsNumber"></param>
    /// <param name="DemographicFunctionURI"></param>
    /// <returns></returns>
    public async Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI)
    {
        var url = $"{DemographicFunctionURI}?Id={NhsNumber}";

        var response = await _httpClientFunction.SendGet(url);
        var demographicData = JsonSerializer.Deserialize<Demographic>(response);

        return demographicData;
    }
}
