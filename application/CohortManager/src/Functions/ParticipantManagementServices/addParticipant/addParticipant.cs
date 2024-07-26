using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using Model;
using NHS.CohortManager.CohortDistribution;
namespace addParticipant
{
    public class AddParticipantFunction
    {
        private readonly ILogger<AddParticipantFunction> _logger;
        private readonly ICallFunction _callFunction;
        private readonly ICreateResponse _createResponse;
        private readonly ICheckDemographic _getDemographicData;
        private readonly ICreateParticipant _createParticipant;
        private readonly IExceptionHandler _handleException;

        public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException)
        {
            _logger = logger;
            _callFunction = callFunction;
            _createResponse = createResponse;
            _getDemographicData = checkDemographic;
            _createParticipant = createParticipant;
            _handleException = handleException;
        }

        [Function("addParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# addParticipant called.");
            HttpWebResponse createResponse, eligibleResponse;

            string postData = "";
            Participant participant = new Participant();
            BasicParticipantCsvRecord basicParticipantCsvRecord = new BasicParticipantCsvRecord();
            try
            {
                using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    postData = reader.ReadToEnd();
                }
                basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(postData);


                var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));
                if (demographicData == null)
                {
                    _logger.LogInformation("demographic function failed");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
                }

                participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
                participant.ScreeningId = "BSS"; // TEMP HARD CODING WILL NEED TO BE TAKEN FROM FILENAME WHEN READY
                var participantCsvRecord = new ParticipantCsvRecord
                {
                    Participant = participant,
                    FileName = basicParticipantCsvRecord.FileName,
                };
                participantCsvRecord.Participant.ExceptionFlag = "N";
                var response = await ValidateData(participantCsvRecord);
                if (response.Participant.ExceptionFlag == "Y")
                {
                    participantCsvRecord = response;
                }

                var json = JsonSerializer.Serialize(participantCsvRecord);
                createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

                if (createResponse.StatusCode != HttpStatusCode.OK)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError,req);
                }
                _logger.LogInformation("participant created");



                var participantJson = JsonSerializer.Serialize(participant);
                eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), participantJson);

                if (eligibleResponse.StatusCode != HttpStatusCode.OK)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError,req);
                }
                _logger.LogInformation("participant created, marked as eligible");


                if(!await SendToCohortDistributionService(participant.NhsNumber,participant.ScreeningId))
                {
                    _logger.LogInformation("participant failed to send to Cohort Distribution Service");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError,req);
                }
                _logger.LogInformation("participant sent to Cohort Distribution Service");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);



            }
            catch(Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
                await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError,req);
            }
        }

        private async Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService){
            CreateCohortDistributionRequestBody requestBody = new CreateCohortDistributionRequestBody{
                NhsNumber = nhsNumber,
                ScreeningService = screeningService
            };
            string json = JsonSerializer.Serialize(requestBody);

            var result = await _callFunction.SendPost(Environment.GetEnvironmentVariable("CohortDistributionServiceURL"), json);

            if(result.StatusCode == HttpStatusCode.OK){
                _logger.LogInformation($"Participant sent to Cohort Distribution Service");
                return true;
            }
            _logger.LogWarning("Unable to send participant to Cohort Distribution Service");
            return false;

        }
        private async Task<ParticipantCsvRecord> ValidateData(ParticipantCsvRecord participantCsvRecord)
        {
            var json = JsonSerializer.Serialize(participantCsvRecord);

            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);
            if (response.StatusCode == HttpStatusCode.Created)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }
            return participantCsvRecord;
        }
    }
}
