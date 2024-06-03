using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

namespace DemographicDataManagementFunction
{
    public class DemographicDataFunction
    {
        private readonly ILogger<DemographicDataFunction> _logger;
        private readonly ICreateResponse _createResponse;
        private readonly ICallFunction _callFunction;

        public DemographicDataFunction(ILogger<DemographicDataFunction> logger, ICreateResponse createResponse, ICallFunction callFunction)
        {
            _logger = logger;
            _createResponse = createResponse;
            _callFunction = callFunction;
        }

        [Function("DemographicDataFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string requestBody = "";
            var participantData = new Participant();
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantData = JsonSerializer.Deserialize<Participant>(requestBody);
            }

            var res = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DemographicDataFunctionURI"), participantData.NHSId);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(res.StatusCode, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}
