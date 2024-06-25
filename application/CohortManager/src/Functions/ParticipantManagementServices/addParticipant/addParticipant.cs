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
        private readonly ICheckDemographic _getDemographicData;
        private readonly ICreateParticipant _createParticipant;

        public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateParticipant createParticipant)
        {
            _logger = logger;
            _callFunction = callFunction;
            _createResponse = createResponse;
            _getDemographicData = checkDemographic;
            _createParticipant = createParticipant;
        }

        [Function("addParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# addParticipant called.");
            HttpWebResponse createResponse, eligibleResponse;

            string postData = "";
            Participant participant = new Participant();
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                postData = reader.ReadToEnd();
            }
            var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(postData);

            try
            {
                var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));
                if (demographicData == null)
                {
                    _logger.LogInformation("demographic function failed");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
                }

                participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
                var participantCsvRecord = new ParticipantCsvRecord
                {
                    Participant = participant,
                    FileName = basicParticipantCsvRecord.FileName,
                };
                var json = JsonSerializer.Serialize(participantCsvRecord);

                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

                if (createResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
            try
            {
                var json = JsonSerializer.Serialize(participant);
                eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), json);

                if (eligibleResponse.StatusCode == HttpStatusCode.Created)
                {
                    _logger.LogInformation("participant created, marked as eligible");
                    _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}
