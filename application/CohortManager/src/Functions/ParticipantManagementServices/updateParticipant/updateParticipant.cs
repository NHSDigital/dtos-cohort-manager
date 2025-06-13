namespace updateParticipant;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model;
using System.Text.Json;
using Common;
using NHS.Screening.UpdateParticipant;
using Microsoft.Extensions.Options;

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly UpdateParticipantConfig _config;

    public UpdateParticipantFunction(
        ILogger<UpdateParticipantFunction> logger,
        IHttpClientFunction httpClientFunction,
        ICheckDemographic checkDemographic,
        ICreateParticipant createParticipant,
        IExceptionHandler handleException,
        ICohortDistributionHandler cohortDistributionHandler,
        IOptions<UpdateParticipantConfig> updateParticipantConfig)
    {
        _logger = logger;
        _httpClientFunction = httpClientFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
        _config = updateParticipantConfig.Value;
    }

    [Function("updateParticipant")]
    public async Task Run([QueueTrigger("%UpdateQueueName%", Connection = "AzureWebJobsStorage")] string jsonFromQueue)
    {
        _logger.LogInformation("Update participant called.");

        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);

        try
        {
            var demographicData = await _checkDemographic.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, _config.DemographicURIGet);

            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName
            };

            participantCsvRecord.Participant.ExceptionFlag = "N";
            var response = await ValidateData(participantCsvRecord);

            if (response.IsFatal)
            {
                await HandleExceptions("A fatal Rule was violated and therefore the record cannot be added to the database", basicParticipantCsvRecord);
                return;
            }

            if (response.CreatedException)
                participantCsvRecord.Participant.ExceptionFlag = "Y";

            if (!await UpdateParticipant(participantCsvRecord))
            {
                await HandleExceptions("There was problem posting the participant to the database", basicParticipantCsvRecord);
                return;
            }

            if (!await SendToCohortDistribution(participant, participantCsvRecord.FileName))
            {
                await HandleExceptions("Failed to send record to cohort distribution", basicParticipantCsvRecord);
            }

            _logger.LogInformation("participant sent to Cohort Distribution Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
        }
    }

    private async Task HandleExceptions(string message, BasicParticipantCsvRecord participantRecord)
    {
        _logger.LogError(message);
        await _handleException.CreateSystemExceptionLog(new SystemException(message), participantRecord.Participant, participantRecord.FileName);
    }

    private async Task<bool> SendToCohortDistribution(Participant participant, string fileName)
    {
        if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, fileName, participant))
        {
            _logger.LogInformation("Participant failed to send to Cohort Distribution Service");
            return false;
        }
        return true;
    }

    private async Task<bool> UpdateParticipant(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        var createResponse = await _httpClientFunction.SendPost(_config.UpdateParticipant, json);
        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Participant updated.");
            return true;
        }
        return false;
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        try
        {
            if (string.IsNullOrWhiteSpace(participantCsvRecord.Participant.ScreeningName))
            {
                var errorDescription = $"A record with Nhs Number: {participantCsvRecord.Participant.NhsNumber} has invalid screening name and therefore cannot be processed by the static validation function";
                await _handleException.CreateRecordValidationExceptionLog(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.FileName, errorDescription, "", JsonSerializer.Serialize(participantCsvRecord.Participant));

                return new ValidationExceptionLog()
                {
                    IsFatal = false,
                    CreatedException = true
                };
            }

            var response = await _httpClientFunction.SendPost(_config.StaticValidationURL, json);
            var responseBodyJson = await _httpClientFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Static validation failed.\nMessage: {Message}\nParticipant: REDACTED", ex.Message);
            return null;
        }
    }
}

