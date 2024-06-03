using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

namespace RemoveParticipant
{
    public class RemoveParticipantFunction
    {
        private readonly ILogger<RemoveParticipantFunction> _logger;
        private readonly ICreateResponse _createResponse;
        private ICallFunction _callFunction;

        private readonly ICheckDemographic _checkDemographic;

        public RemoveParticipantFunction(ILogger<RemoveParticipantFunction> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic)
        {
            _logger = logger;
            _createResponse = createResponse;
            _callFunction = callFunction;
            _checkDemographic = checkDemographic;
        }

        [Function("RemoveParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("C# addParticipant called.");
                HttpWebResponse createResponse;

                // convert body to json and then deserialize to object
                string postdata = "";
                using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    postdata = reader.ReadToEnd();
                }
                var input = JsonSerializer.Deserialize<Participant>(postdata);
                if (!await _checkDemographic.CheckDemographicAsync(input.NHSId, Environment.GetEnvironmentVariable("DemographicURI")))
                {
                    _logger.LogInformation("demographic function failed");
                }
                // Any validation or decisions go in here

                // call data service create Participant

                var json = JsonSerializer.Serialize(input);
                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("markParticipantAsIneligible"), json);

                if (createResponse.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation("participant deleted");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
