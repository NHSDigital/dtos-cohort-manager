using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using Model;

namespace addParticipant
{
    public class AddParticipantFunction
    {
        private readonly ILogger<AddParticipantFunction> _logger;
        private readonly ICallFunction _callFunction;

        private readonly ICreateResponse _createResponse;

        private readonly ICheckDemographic _checkDemographic;

        public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic)
        {
            _logger = logger;
            _callFunction = callFunction;
            _createResponse = createResponse;
            _checkDemographic = checkDemographic;
        }

        [Function("addParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# addParticipant called.");
            HttpWebResponse createResponse, eligibleResponse;

            // convert body to json and then deserialize to object
            string postdata = "";
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                postdata = reader.ReadToEnd();
            }
            Participant input = JsonSerializer.Deserialize<Participant>(postdata);

            // Any validation or decisions go in here

            // call data service create Participant
            try
            {
                if (!await _checkDemographic.CheckDemographicAsync(input.NHSId, Environment.GetEnvironmentVariable("DemographicURI")))
                {
                    _logger.LogInformation("demographic function failed");
                }
                var json = JsonSerializer.Serialize(input);
                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);
                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

                if (createResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            // call data service mark as eligible
            try
            {
                var json = JsonSerializer.Serialize(input);
                eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), json);

                if (eligibleResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created, marked as eligible");
                    _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}
