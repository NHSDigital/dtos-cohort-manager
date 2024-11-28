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
using DataServices.Client;
using Microsoft.EntityFrameworkCore.Query;

public class CreateParticipant
{
    private readonly ILogger<CreateParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly ICallFunction _callFunction;
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public CreateParticipant(ILogger<CreateParticipant> logger,
        ICreateResponse createResponse,
        IExceptionHandler handleException,
        ICallFunction callFunction,
        IDataServiceClient<ParticipantManagement> participantManagementClient
        )
    {
        _logger = logger;
        _createResponse = createResponse;
        _handleException = handleException;
        _callFunction = callFunction;
        _participantManagementClient = participantManagementClient;
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

            // var existingParticipantData = _participantManagerData.GetParticipant(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.Participant.ScreeningId);

            var existingParticipantResult = await _participantManagementClient.GetByFilter(i => i.NHSNumber.ToString() == participantCsvRecord.Participant.NhsNumber && i.ScreeningId.ToString() == participantCsvRecord.Participant.ScreeningId);
            Participant existingParticipant = new Participant();

            if(existingParticipantResult != null && existingParticipantResult.Any())
            {
                existingParticipant = new Participant(existingParticipantResult.FirstOrDefault());
            }



            var response = await ValidateData(existingParticipant, participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: {NhsNumber}", participantCsvRecord.Participant.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }




            var participantCreated = await  _participantManagementClient.Add(new ParticipantManagement{
                ScreeningId = Int64.Parse(participantCsvRecord.Participant.ScreeningId),
                NHSNumber = Int64.Parse(participantCsvRecord.Participant.NhsNumber),
                ReasonForRemoval = participantCsvRecord.Participant.ReasonForRemoval,
                ReasonForRemovalDate = ParseNullableDateTime(participantCsvRecord.Participant.ReasonForRemovalEffectiveFromDate),
                BusinessRuleVersion = participantCsvRecord.Participant.BusinessRuleVersion,
                ExceptionFlag = participantCsvRecord.Participant.ExceptionFlag == "Y" ? Int16.Parse("1") : Int16.Parse("0"),
                RecordInsertDateTime = ParseNullableDateTime(participantCsvRecord.Participant.RecordInsertDateTime),
                RecordUpdateDateTime = ParseNullableDateTime(participantCsvRecord.Participant.RecordUpdateDateTime),
                RecordType = participantCsvRecord.Participant.RecordType

            });



            if (participantCreated)
            {
                _logger.LogInformation("Successfully created the participant");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            _logger.LogError("Failed to create the participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Failed to make the CreateParticipant request\nMessage: {Message}", ex.Message);
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
            _logger.LogInformation(ex,$"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {newParticipant}");
            return null;
        }
    }

    private DateTime? ParseNullableDateTime(string dateTimeString)
    {
        if(DateTime.TryParse(dateTimeString,out var result))
        {
            return result;
        }
        return null;
    }
}
