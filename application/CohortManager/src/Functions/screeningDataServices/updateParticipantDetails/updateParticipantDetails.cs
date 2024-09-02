namespace updateParticipantDetails;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class UpdateParticipantDetails
{
    private readonly ILogger<UpdateParticipantDetails> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IParticipantManagerData _participantManagerData;
    private readonly IExceptionHandler _handleException;

    private readonly ICallFunction _callFunction;

    public UpdateParticipantDetails(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse, IParticipantManagerData participantManagerData, IExceptionHandler handleException, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _participantManagerData = participantManagerData;
        _handleException = handleException;
        _callFunction = callFunction;
    }

    [Function("updateParticipantDetails")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participantCsvRecord = new ParticipantCsvRecord();
        try
        {
            string requestBody = "";

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            var existingParticipantData = _participantManagerData.GetParticipant(participantCsvRecord.Participant.NhsNumber);
            var response = await ValidateData(existingParticipantData, participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: {NhsNumber}", participantCsvRecord.Participant.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }

            var isAdded = await _participantManagerData.UpdateParticipantDetails(participantCsvRecord, existingParticipantData);
            if (isAdded)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord.Participant, participantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
    private async Task<ValidationExceptionLog> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName, Model.Enums.RulesType.ParticipantManagement));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {newParticipant}");
            return null;
        }
    }
}
