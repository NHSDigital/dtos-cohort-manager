namespace updateParticipant;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using Model;
using System.Text.Json;
using Common;
using System.Data;
using NHS.CohortManager.CohortDistribution;
using Microsoft.Identity.Client;

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;

    public UpdateParticipantFunction(ILogger<UpdateParticipantFunction> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException, ICohortDistributionHandler cohortDistributionHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
    }

    [Function("updateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Update participant called.");
        HttpWebResponse createResponse;

        string postData = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postData = reader.ReadToEnd();
        }
        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(postData);

        try
        {
            var demographicData = await _checkDemographic.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName
            };

            participantCsvRecord.Participant.ExceptionFlag = "N";
            var response = await ValidateData(participantCsvRecord);

            var responseDataFromCohort = false;
            var updateResponse = false;
            if (response.Participant.ExceptionFlag == "Y")
            {
                participantCsvRecord = response;
                updateResponse = await updateParticipant(participantCsvRecord, req);
                _logger.LogInformation("The participant has not been updated but a validation Exception was raised");
                responseDataFromCohort = await SendToCohortDistribution(participant, participantCsvRecord.FileName, req);

                return updateResponse && responseDataFromCohort ? _createResponse.CreateHttpResponse(HttpStatusCode.OK, req) : _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            updateResponse = await updateParticipant(participantCsvRecord, req);
            responseDataFromCohort = await SendToCohortDistribution(participant, participantCsvRecord.FileName, req);

            _logger.LogInformation("participant sent to Cohort Distribution Service");
            return updateResponse && responseDataFromCohort ? _createResponse.CreateHttpResponse(HttpStatusCode.OK, req) : _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);

        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }


    }

    private async Task<bool> SendToCohortDistribution(Participant participant, string fileName, HttpRequestData req)
    {
        if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, fileName))
        {
            _logger.LogInformation("participant failed to send to Cohort Distribution Service");
            return false;
        }
        return true;
    }

    private async Task<bool> updateParticipant(ParticipantCsvRecord participantCsvRecord, HttpRequestData req)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("UpdateParticipant"), json);
        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Participant updated.");
            return true;
        }
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

