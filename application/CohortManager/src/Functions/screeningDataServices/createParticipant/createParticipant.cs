namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Data.Database;
using Common;
using Model;

public class CreateParticipant
{
    private readonly ILogger<CreateParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICreateParticipantData _createParticipantData;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;
    private readonly IParticipantManagerData _participantManagerData;

    public CreateParticipant(ILogger<CreateParticipant> logger, ICreateResponse createResponse, ICreateParticipantData createParticipantData, IExceptionHandler handleException, IParticipantManagerData participantManagerData, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _createParticipantData = createParticipantData;
        _handleException = handleException;
        _participantManagerData = participantManagerData;
        _callFunction = callFunction;
    }

    [Function("CreateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("CreateParticipant is called...");
        ParticipantCsvRecord participantCsvRecord = null;
        try
        {
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
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

            var participantCreated = await _createParticipantData.CreateParticipantEntry(participantCsvRecord);
            if (participantCreated)
            {
                _logger.LogInformation("Successfully created the participant");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            _logger.LogError("Failed to create the participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to make the CreateParticipant request\nMessage: {Message}", ex.Message);
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
