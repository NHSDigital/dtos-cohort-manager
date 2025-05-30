namespace Common;

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Polly;

public class CheckDemographic : ICheckDemographic
{
    private readonly ICallFunction _callFunction;

    public CheckDemographic(ICallFunction callFunction)
    {
        _callFunction = callFunction;

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

        var response = await _callFunction.SendGet(url);

        var demographicData = JsonSerializer.Deserialize<Demographic>(response);

        return demographicData;
    }
}
