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

        public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse)
        {
            _logger = logger;
            _callFunction = callFunction;
            _createResponse = createResponse;
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
            // Participant input = JsonSerializer.Deserialize<Participant>(postdata);

            // Any validation or decisions go in here

            // call data service create Participant
            try
            {
                // var json = JsonSerializer.Serialize(input);
                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), postdata);

                if (createResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created");
                } else {
                    throw new Exception("Failed to create particpant");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            // call data service mark as eligible
            try
            {
                //var json = JsonSerializer.Serialize(input);
                eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), postdata);

                if (eligibleResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created, marked as eligible");
                } else {
                    throw new Exception("Failed to mark as elibible");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}
