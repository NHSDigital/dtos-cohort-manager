
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;

namespace Common
{
    public class CheckDemographic : ICheckDemographic
    {
        private readonly ICallFunction _callFunction;
        public readonly ILogger _logger;
        public CheckDemographic(ICallFunction callFunction, ILogger logger)
        {
            _callFunction = callFunction;
            _logger = logger;
        }

        public async Task<Demographic> CheckDemographicAsync(string NhsId, string DemographicFunctionURI)
        {
            var DemographicData = await _callFunction.SendGet(DemographicFunctionURI, JsonSerializer.Serialize(NhsId));
            if (DemographicData.StatusCode == HttpStatusCode.OK)
            {
                var demographicDataResponse = new Demographic();
                _logger.LogError($"The demographic function has failed with NHSId {NhsId}");
                using (var reader = new StreamReader(DemographicData.GetResponseStream()))
                {
                    var dataRead = reader.ReadToEnd();

                    demographicDataResponse = JsonSerializer.Deserialize<Demographic>(dataRead);
                }
                return demographicDataResponse;
            }
            return null;
        }
    }
}