namespace updateParticipant;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using System.Text.Json;
using Common;

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;

    public UpdateParticipantFunction(ILogger<UpdateParticipantFunction> logger, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException, ICohortDistributionHandler cohortDistributionHandler)
    {
        _logger = logger;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
    }

    [Function("updateParticipant")]
    public async Task Run([QueueTrigger("%UpdateQueueName%", Connection = "AzureWebJobsStorage")] string jsonFromQueue)
    {
        _logger.LogInformation("Update participant called.");

        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);

        try
        {
            var demographicData = await _checkDemographic.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                return;
            }
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
                _logger.LogError("A fatal Rule was violated and therefore the record cannot be added to the database");
                return;
            }

            var responseDataFromCohort = false;
            var updateResponse = false;
            var participantEligibleResponse = false;
            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
                updateResponse = await UpdateParticipant(participantCsvRecord);
                if (!updateResponse)
                {
                    _logger.LogInformation("unsuccessfully updated records");
                    return;
                }
                
                participantEligibleResponse = await MarkParticipantAsEligible(participantCsvRecord);

                _logger.LogInformation("The participant has been updated but a validation Exception was raised");
                responseDataFromCohort = await SendToCohortDistribution(participant, participantCsvRecord.FileName);


                LogResultFromUpdating(updateResponse, responseDataFromCohort, participantEligibleResponse);
                return;
            }

            updateResponse = await UpdateParticipant(participantCsvRecord);
            if (!updateResponse)
            {
                _logger.LogInformation("unsuccessfully updated records");
                return;
            }
            participantEligibleResponse = await MarkParticipantAsEligible(participantCsvRecord);
            responseDataFromCohort = await SendToCohortDistribution(participant, participantCsvRecord.FileName);

            _logger.LogInformation("participant sent to Cohort Distribution Service");
            LogResultFromUpdating(updateResponse, responseDataFromCohort, participantEligibleResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
        }
    }

    private void LogResultFromUpdating(bool updateResponse, bool responseDataFromCohort, bool participantEligibleResponse)
    {
        if (updateResponse && responseDataFromCohort && participantEligibleResponse)
        {
            _logger.LogInformation("successfully updated records");
        }
        else
        {
            _logger.LogError("Unsuccessfully updated records with one of the functions failing. UpdateResponse: {updateResponse},ResponseDataFromCohort {responseDataFromCohort}, ParticipantEligibleResponse {participantEligibleResponse} ",
            updateResponse, responseDataFromCohort, participantEligibleResponse);
        }
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

        var createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("UpdateParticipant"), json);
        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Participant updated.");
            return true;
        }
        return false;
    }

    private async Task<bool> MarkParticipantAsEligible(ParticipantCsvRecord participantCsvRecord)
    {
        HttpWebResponse eligibilityResponse;

        if (participantCsvRecord.Participant.EligibilityFlag == EligibilityFlag.Eligible)
        {
            var participantJson = JsonSerializer.Serialize(participantCsvRecord.Participant);
            eligibilityResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), participantJson);
        }
        else
        {
            var participantJson = JsonSerializer.Serialize(participantCsvRecord);
            eligibilityResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("markParticipantAsIneligible"), participantJson);
        }

        if (eligibilityResponse.StatusCode == HttpStatusCode.OK)
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

            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
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

