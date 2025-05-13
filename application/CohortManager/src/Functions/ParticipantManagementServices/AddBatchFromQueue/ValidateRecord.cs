namespace AddBatchFromQueue;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;


public class ValidateRecord : IValidateRecord
{

    private readonly ILogger<DurableAddProcessor> _logger;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;
    private readonly AddBatchFromQueueConfig _config;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;


    public ValidateRecord(
        IOptions<AddBatchFromQueueConfig> config,
        ILogger<DurableAddProcessor> logger,
        IExceptionHandler handleException,
        ICallFunction callFunction,
        IDataServiceClient<ParticipantManagement> participantManagementClient
    )
    {
        _logger = logger;
        _handleException = handleException;
        _callFunction = callFunction;
        _config = config.Value;
        _participantManagementClient = participantManagementClient;
    }
    public async Task<(Participant Participant, ValidationExceptionLog ValidationExceptionLog)> ValidateData(ParticipantCsvRecord participantCsvRecord, Participant participant)
    {
        // Validation
        ValidationExceptionLog validationExceptionLog;
        participantCsvRecord.Participant.ExceptionFlag = "N";
        participant.ExceptionFlag = "N";

        var json = JsonSerializer.Serialize(participantCsvRecord);

        if (string.IsNullOrWhiteSpace(participantCsvRecord.Participant.ScreeningName))
        {
            var errorDescription = $"A record with Nhs Number: {participantCsvRecord.Participant.NhsNumber} has invalid screening name and therefore cannot be processed by the static validation function";
            await _handleException.CreateRecordValidationExceptionLog(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.FileName, errorDescription, "", JsonSerializer.Serialize(participantCsvRecord.Participant));

            validationExceptionLog = new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = true
            };
            return (null, validationExceptionLog)!;
        }

        var response = await _callFunction.SendPost(_config.StaticValidationURL, json);
        var responseBodyJson = await _callFunction.GetResponseText(response);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        if (responseBody.IsFatal)
        {
            _logger.LogError("A fatal Rule was violated, so the record cannot be added to the database");
            await _handleException.CreateSystemExceptionLog(null, participantCsvRecord.Participant, participantCsvRecord.FileName);
            return (null, responseBody)!;
        }

        if (responseBody.CreatedException)
        {
            participantCsvRecord.Participant.ExceptionFlag = "Y";
            participant.ExceptionFlag = "Y";
        }


        return (participant, responseBody);
    }

    public async Task<ParticipantCsvRecord> ValidateLookUpData(ParticipantCsvRecord newParticipantCsvRecord, string fileName)
    {
        var existingParticipant = new Participant();
        long screeningId;
        long nhsNumber;

        if (!long.TryParse(newParticipantCsvRecord.Participant.ScreeningId, out screeningId))
            throw new FormatException("Could not parse ScreeningId");

        if (!long.TryParse(newParticipantCsvRecord.Participant.NhsNumber, out nhsNumber))
            throw new FormatException("Could not parse NhsNumber");

        var existingParticipantResult = await _participantManagementClient.GetByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);

        if (existingParticipantResult != null && existingParticipantResult.Any())
            existingParticipant = new Participant(existingParticipantResult.First());

        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipantCsvRecord.Participant, fileName, Model.Enums.RulesType.ParticipantManagement));

        try
        {
            var response = await _callFunction.SendPost(_config.LookupValidationURL, json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);


            if (responseBody!.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: REDACTED");
                throw new Exception("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: REDACTED");
            }

            if (responseBody.CreatedException)
                newParticipantCsvRecord.Participant.ExceptionFlag = "Y";


            return newParticipantCsvRecord;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Lookup validation failed.\nMessage: {Message}\nParticipant: REDACTED", ex.Message);
            return null;
        }
    }


}