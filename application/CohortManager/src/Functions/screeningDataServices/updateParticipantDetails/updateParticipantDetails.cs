namespace updateParticipantDetails;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using DataServices.Client;

public class UpdateParticipantDetails
{
    private readonly ILogger<UpdateParticipantDetails> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    public UpdateParticipantDetails(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse,
                                    IExceptionHandler handleException, ICallFunction callFunction,
                                    IDataServiceClient<ParticipantManagement> participantManagementClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _handleException = handleException;
        _callFunction = callFunction;
        _participantManagementClient = participantManagementClient;
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

            Participant reqParticipant = participantCsvRecord.Participant;


            long nhsNumberLong;
            if (!long.TryParse(reqParticipant.NhsNumber, out nhsNumberLong))
            {
                throw new FormatException("Could not parse Long in update participant details");
            }

            long ScreeningIdLong;
            if (!long.TryParse(reqParticipant.ScreeningId, out ScreeningIdLong))
            {
                throw new FormatException("Could not parse Long in update participant details");
            }

            var existingParticipantData = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == nhsNumberLong
                                                                                        && p.ScreeningId == ScreeningIdLong);

            var response = await ValidateData(new Participant(existingParticipantData), participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: {ParticipantId}", participantCsvRecord.Participant.ParticipantId);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
            {
                _logger.LogInformation("Validation Error: A Rule was violated but it was not Fatal for record with Participant Id: {ParticipantId}", participantCsvRecord.Participant.ParticipantId);
                reqParticipant.ExceptionFlag = "1";
            }

            reqParticipant.ParticipantId = existingParticipantData.ParticipantId.ToString();
            var isAdded = await _participantManagementClient.Update(reqParticipant.ToParticipantManagement());

            if (isAdded)
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message, ex);
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
            _logger.LogInformation(ex, "Lookup validation failed.\nMessage: {Message}\nParticipant: REDACTED", ex.Message);
            return null;
        }
    }
}
