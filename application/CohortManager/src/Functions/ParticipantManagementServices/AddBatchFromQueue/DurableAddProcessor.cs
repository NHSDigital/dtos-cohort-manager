namespace AddBatchFromQueue;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class DurableAddProcessor : IDurableAddProcessor
{
    private readonly ILogger<DurableAddProcessor> _logger;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly AddBatchFromQueueConfig _config;
    private readonly IValidateRecord _validateRecord;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;


    public DurableAddProcessor(
        IOptions<AddBatchFromQueueConfig> config,
        ILogger<DurableAddProcessor> logger,
        ICheckDemographic checkDemographic,
        ICreateParticipant createParticipant,
        IExceptionHandler handleException,
        IValidateRecord validateRecord,
        IDataServiceClient<ParticipantManagement> participantManagementClient
    )
    {
        _logger = logger;
        _getDemographicData = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _config = config.Value;
        _validateRecord = validateRecord;
        _participantManagementClient = participantManagementClient;
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

            //validate record and set EligibilityFlag 
            (participantCsvRecord, participant) = await ValidateData(participantCsvRecord, participant);
            if (participantCsvRecord == null || participant == null)
            {
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

    private async Task<(ParticipantCsvRecord participantCsvRecord, Participant participant)> ValidateData(ParticipantCsvRecord participantCsvRecord, Participant participant)
    {
        var response = await _validateRecord.ValidateData(participantCsvRecord, participant);
        if (response.ValidationExceptionLog.IsFatal)
        {
            return (null, null)!;
        }
        participant = response.Participant;

        var validateLookUpResult = await _validateRecord.ValidateLookUpData(participantCsvRecord, participantCsvRecord.FileName);
        if (validateLookUpResult == null)
        {
            _logger.LogError("The validateLookUpResult was null");
            return (null, null)!;
        }
        participantCsvRecord = validateLookUpResult;

        return (participantCsvRecord, participant);
    }


}

