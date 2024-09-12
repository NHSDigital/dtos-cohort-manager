namespace addParticipant;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using Model;

public class AddParticipantFunction
{
    private readonly ILogger<AddParticipantFunction> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;

    public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException, ICohortDistributionHandler cohortDistributionHandler)
    {
        _logger = logger;
        _callFunction = callFunction;
        _createResponse = createResponse;
        _getDemographicData = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
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
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };
            participantCsvRecord.Participant.ExceptionFlag = "N";
            var response = await ValidateData(participantCsvRecord);
            if (response.IsFatal)
            {
                _logger.LogError("A fatal Rule was violated and therefore the record cannot be added to the database");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }


            var json = JsonSerializer.Serialize(participantCsvRecord);
            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
            _logger.LogInformation("participant created");

            var participantJson = JsonSerializer.Serialize(participant);
            eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), participantJson);

            if (eligibleResponse.StatusCode != HttpStatusCode.OK)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
            _logger.LogInformation("participant created, marked as eligible");


            if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, basicParticipantCsvRecord.FileName))
            {
                _logger.LogInformation("participant failed to send to Cohort Distribution Service");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
            _logger.LogInformation("participant sent to Cohort Distribution Service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Static validation failed.\nMessage: {ex.Message}\nParticipant: {participantCsvRecord}");
            return null;
        }
    }
}
