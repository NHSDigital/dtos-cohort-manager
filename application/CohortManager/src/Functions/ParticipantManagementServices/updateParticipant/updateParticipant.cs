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

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;

    public UpdateParticipantFunction(ILogger<UpdateParticipantFunction> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
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
            if (response.Participant.ExceptionFlag == "Y")
            {
                participantCsvRecord = response;
                await updateParticipant(participantCsvRecord, req);
                _logger.LogInformation("The participant has not been updated but a validation Exception was raised");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            await updateParticipant(participantCsvRecord, req);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }


    }

    private async Task<HttpWebResponse> updateParticipant(ParticipantCsvRecord participantCsvRecord, HttpRequestData req)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);
        HttpWebResponse createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("UpdateParticipant"), json);
        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Participant updated.");
            return createResponse;
        }
        return createResponse;
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

