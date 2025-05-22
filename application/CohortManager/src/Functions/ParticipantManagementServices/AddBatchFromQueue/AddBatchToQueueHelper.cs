namespace AddBatchFromQueue;


using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class AddBatchFromQueueHelper : IAddBatchFromQueueHelper
{
    ILogger<AddBatchFromQueueHelper> _logger;
    ICohortDistributionHandler _cohortDistributionHandler;
    IExceptionHandler _handleException;
    private readonly AddBatchFromQueueConfig _config;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;

    private readonly IValidateRecord _validateRecord;

    public AddBatchFromQueueHelper(
        ILogger<AddBatchFromQueueHelper> logger,
        ICohortDistributionHandler cohortDistributionHandler,
        IExceptionHandler handleException,
        ICheckDemographic checkDemographic,
        ICreateParticipant createParticipant,
        IValidateRecord validateRecord,
        IOptions<AddBatchFromQueueConfig> config
        )
    {
        _logger = logger;
        _cohortDistributionHandler = cohortDistributionHandler;
        _handleException = handleException;
        _createParticipant = createParticipant;
        _getDemographicData = checkDemographic;
        _validateRecord = validateRecord;
        _config = config.Value;
    }

    public async Task<ParticipantCsvRecord?> GetDemoGraphicData(string jsonFromQueue)
    {
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
            return participantCsvRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// process a single record 
    /// </summary>
    /// <param name="jsonFromQueue"></param>
    /// <returns></returns>
    public async Task<ParticipantCsvRecord?> ValidateMessageFromQueue(ParticipantCsvRecord participantCsvRecord)
    {
        Participant participant;
        try
        {
            participant = participantCsvRecord.Participant;
            //validate record and set EligibilityFlag 
            (participantCsvRecord, participant) = await _validateRecord.ValidateData(participantCsvRecord, participant);
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
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord.Participant, participantCsvRecord.FileName);
            return null;
        }
    }

    public async Task AddAllCohortRecordsToQueue(List<ParticipantCsvRecord> participantsData)
    {
        foreach (var ParticipantCsvRecord in participantsData)
        {
            var cohortDistResponse = await _cohortDistributionHandler.SendToCohortDistributionService(ParticipantCsvRecord.Participant.NhsNumber!, ParticipantCsvRecord.Participant.ScreeningId!, ParticipantCsvRecord.Participant.RecordType!, ParticipantCsvRecord.FileName, ParticipantCsvRecord.Participant);
            if (!cohortDistResponse)
            {
                _logger.LogError("Participant failed to send to Cohort Distribution Service");
                await _handleException.CreateSystemExceptionLog(new Exception("participant failed to send to Cohort Distribution Service"), ParticipantCsvRecord.Participant, ParticipantCsvRecord.FileName);
                return;
            }
        }
    }
}

