namespace updateParticipant;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using Model;
using System.Text.Json;
using Common;

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    private readonly ICheckDemographic _checkDemographic;

    private readonly ICreateParticipant _createParticipant;

    public UpdateParticipantFunction(ILogger<UpdateParticipantFunction> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateParticipant createParticipant)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
    }

    [Function("updateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Update participant called.");
        HttpWebResponse createResponse;

        // convert body to json and then deserialize to object
        string postdata = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postdata = reader.ReadToEnd();
        }
        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(postdata);

        try
        {
            var demographicData = await _checkDemographic.GetDemographicAsync(basicParticipantCsvRecord.Participant.NHSId, Environment.GetEnvironmentVariable("DemographicURIGet"));
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var json = JsonSerializer.Serialize(participant);

            if (!await ValidateData(participant, basicParticipantCsvRecord.FileName))
            {
                _logger.LogInformation("The participant has not been updated due to a bad request.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("UpdateParticipant"), json);

            if (createResponse.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Participant updated.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        _logger.LogInformation("The participant has not been updated due to a bad request.");
        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
    }

    private async Task<bool> ValidateData(Participant participant, string fileName)
    {
        var participantCsvRecord = new ParticipantCsvRecord
        {
            Participant = participant,
            FileName = fileName
        };

        var json = JsonSerializer.Serialize(participantCsvRecord);

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Static validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return false;
        }

        return false;
    }
}
