namespace Common;

using System.Net;
using System.Text.Json;
using Model;

public class CheckDemographic : ICheckDemographic
{
    private readonly ICallFunction _callFunction;

    public CheckDemographic(ICallFunction callFunction)
    {
        _callFunction = callFunction;
    }

    public async Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI)
    {
        var url = $"{DemographicFunctionURI}?Id={NhsNumber}";

        var response = await _callFunction.SendGet(url);
        var demographicData = JsonSerializer.Deserialize<Demographic>(response);

        return demographicData;
    }

    public async Task<bool> PostDemographicBatchAsync(List<Participant> participantList, string DemographicFunctionURI)
    {
        var json = JsonSerializer.Serialize(participantList);
        var response = await _callFunction.SendPost(DemographicFunctionURI, json);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }
        return true;
    }
}
