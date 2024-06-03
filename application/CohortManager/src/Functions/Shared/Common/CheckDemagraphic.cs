
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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

        public async Task<bool> CheckDemographicAsync(string NhsId, string DemographicFunctionURI)
        {
            var DemographicCheck = await _callFunction.SendPost(DemographicFunctionURI, JsonSerializer.Serialize(NhsId));
            if (DemographicCheck.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"The demographic function has failed with NHSId {NhsId}");
                return false;
            }
            return true;
        }
    }
}