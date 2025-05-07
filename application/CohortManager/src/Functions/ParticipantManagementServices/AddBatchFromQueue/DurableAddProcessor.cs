namespace AddBatchFromQueue;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class DurableAddProcessor : IDurableAddProcessor
{
    private readonly ILogger<DurableAddProcessor> _logger;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;
    private readonly AddBatchFromQueueConfig _config;


    public DurableAddProcessor(
      IOptions<AddBatchFromQueueConfig> config,
        ILogger<DurableAddProcessor> logger,
        ICheckDemographic checkDemographic,
        ICreateParticipant createParticipant,
        IExceptionHandler handleException,
        ICallFunction callFunction
        )
    {
        _logger = logger;
        _getDemographicData = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _callFunction = callFunction;
        _config = config.Value;
    }

    /// <summary>
    /// process a single record 
    /// </summary>
    /// <param name="jsonFromQueue"></param>
    /// <returns></returns>
    public async Task<ParticipantCsvRecord?> ProcessAddRecord(string jsonFromQueue)
    {
        HttpWebResponse createResponse;
        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);

        try
        {
            // Get demographic data
            var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, _config.DemographicURIGet);
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                await _handleException.CreateSystemExceptionLog(new Exception("demographic function failed"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return null;
            }

            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };

            // Validation
            participantCsvRecord.Participant.ExceptionFlag = "N";
            participant.ExceptionFlag = "N";
            var response = await ValidateData(participantCsvRecord);
            if (response.IsFatal)
            {
                _logger.LogError("A fatal Rule was violated, so the record cannot be added to the database");
                await _handleException.CreateSystemExceptionLog(null, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return null;
            }

            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
                participant.ExceptionFlag = "Y";
            }

            // Add participant to database
            var json = JsonSerializer.Serialize(participantCsvRecord);
            createResponse = await _callFunction.SendPost(_config.DSaddParticipant, json);

            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was problem posting the participant to the database");
                await _handleException.CreateSystemExceptionLog(new Exception("There was problem posting the participant to the database"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return null;

            }
            participant.EligibilityFlag = "1";
            var participantJson = JsonSerializer.Serialize(participant);

            participantCsvRecord.Participant = participant;

            _logger.LogInformation("Participant ready for creation");
            return participantCsvRecord;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Unable to call function.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
            return null;
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

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

        var response = await _callFunction.SendPost(_config.StaticValidationURL, json);
        var responseBodyJson = await _callFunction.GetResponseText(response);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        return responseBody;

    }
}

