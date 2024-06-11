using System.Net;
using System.Text.Json;
using Model;

namespace Common
{
    public class CheckDemographic : ICheckDemographic
    {
        private readonly ICallFunction _callFunction;
        public CheckDemographic(ICallFunction callFunction)
        {
            _callFunction = callFunction;
        }

        public async Task<Demographic> GetDemographicAsync(string NHSId, string DemographicFunctionURI)
        {

            var url = $"{DemographicFunctionURI}?Id={NHSId}";

            var response = await _callFunction.SendGet(url);
            var DemographicData = JsonSerializer.Deserialize<Demographic>(response);

            return DemographicData;
        }

        public async Task<bool> PostDemographicDataAsync(Participant participant, string DemographicFunctionURI)
        {
            var json = JsonSerializer.Serialize(participant);
            var response = await _callFunction.SendPost(DemographicFunctionURI, json);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            return true;
        }
    }
}
