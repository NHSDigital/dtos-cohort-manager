namespace NHS.CohortManager.ParticipantManagementService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Data.Database;

public class RemoveParticipant
{
    private readonly ILogger<RemoveParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateParticipant _createParticipant;
    private readonly IUpdateParticipantData _updateParticipantData;

    public RemoveParticipant(ILogger<RemoveParticipant> logger, ICreateResponse createResponse, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IUpdateParticipantData updateParticipantData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
        _updateParticipantData = updateParticipantData;
    }

    [Function("RemoveParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("C# RemoveParticipant called.");
            HttpWebResponse createResponse;

            string postData = "";
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                postData = reader.ReadToEnd();
            }

            var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(postData);

            var demographicData = await _checkDemographic.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };
            var json = JsonSerializer.Serialize(participantCsvRecord);

            var existingParticipantData = _updateParticipantData.GetParticipant(basicParticipantCsvRecord.Participant.NhsNumber);
            if (!await ValidateData(existingParticipantData, participant, participantCsvRecord.FileName))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("markParticipantAsIneligible"), json);

            if (createResponse.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("participant deleted");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }

    private async Task<bool> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return false;
        }

        return false;
    }
}
