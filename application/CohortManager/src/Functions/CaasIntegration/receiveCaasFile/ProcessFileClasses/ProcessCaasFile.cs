namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;

public class ProcessCaasFile : IProcessCaasFile
{
    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICallDurableDemographicFunc _callDurableDemographicFunc;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;
    private readonly ReceiveCaasFileConfig _config;
    private readonly string DemographicURI;


    public ProcessCaasFile(
        ILogger<ProcessCaasFile> logger,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IExceptionHandler exceptionHandler,
        IDataServiceClient<ParticipantDemographic> participantDemographic,
        IRecordsProcessedTracker recordsProcessedTracker,
        IValidateDates validateDates,
        ICallDurableDemographicFunc callDurableDemographicFunc,
        IOptions<ReceiveCaasFileConfig> receiveCaasFileConfig
    )
    {
        _logger = logger;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _exceptionHandler = exceptionHandler;
        _participantDemographic = participantDemographic;
        _recordsProcessTracker = recordsProcessedTracker;
        _validateDates = validateDates;
        _callDurableDemographicFunc = callDurableDemographicFunc;
        _config = receiveCaasFileConfig.Value;
        DemographicURI = _config.DemographicURI;
    }

    /// <summary>
    /// process a given batch and send it the queue
    /// </summary>
    /// <param name="values"></param>
    /// <param name="options"></param>
    /// <param name="screeningService"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningLkp screeningService, string name)
    {
        var currentBatch = new Batch();
        await Parallel.ForEachAsync(values, options, async (rec, cancellationToken) =>
        {
            var participant = _receiveCaasFileHelper.MapParticipant(rec, screeningService.ScreeningId.ToString(), screeningService.ScreeningName, name);

            if (participant == null)
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new Exception($"Could not map participant in file {name}"), rec.NhsNumber.ToString(), name, screeningService.ScreeningName, "");
                return;
            }

            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Invalid NHS Number was passed in for participant {participant} and file {name}"), participant, name, nameof(ExceptionCategory.CaaS));
                return; // skip current participant
            }

            if (!_validateDates.ValidateAllDates(participant))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Invalid effective date found in participant data {participant} and file name {name}"), participant, name);
                return; // Skip current participant
            }

            if (!_recordsProcessTracker.RecordAlreadyProcessed(participant.RecordType, participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Duplicate Participant was in the file"), participant, name);
                return; // Skip current participant
            }

            await AddRecordToBatch(participant, currentBatch, name);
        });

        if (await _callDurableDemographicFunc.PostDemographicDataAsync(currentBatch.DemographicData.ToList(), DemographicURI, name))
        {
            await AddBatchToQueue(currentBatch, name);
        }
    }

    /// <summary>
    /// adds a given record to the current given batch
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="currentBatch"></param>
    /// <param name="FileName"></param>
    /// <returns></returns>
    private async Task AddRecordToBatch(Participant participant, Batch currentBatch, string fileName)
    {
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = fileName,
            Participant = participant
        };
        // take note: we don't need to add DemographicData to the queue for update because we loop through all updates in the UpdateParticipant method
        switch (participant.RecordType?.Trim())
        {

            case Actions.New:

                currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                if (await UpdateOldDemographicRecord(basicParticipantCsvRecord, fileName))
                {
                    break;
                }
                currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                break;
            case Actions.Amended:
                if (!await UpdateOldDemographicRecord(basicParticipantCsvRecord, fileName))
                {
                    currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                    currentBatch.UpdateRecords.Enqueue(basicParticipantCsvRecord);
                    break;
                }
                currentBatch.UpdateRecords.Enqueue(basicParticipantCsvRecord);
                break;
            case Actions.Removed:
                if (!await UpdateOldDemographicRecord(basicParticipantCsvRecord, fileName))
                {
                    currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                }
                currentBatch.DeleteRecords.Enqueue(basicParticipantCsvRecord);
                break;
            default:
                await _exceptionHandler.CreateSchemaValidationException(basicParticipantCsvRecord, "RecordType was not set to an expected value");
                break;
        }

    }

    private async Task AddBatchToQueue(Batch currentBatch, string name)
    {
        _logger.LogInformation("sending {Count} records to queue", currentBatch.AddRecords.Count + currentBatch.UpdateRecords.Count);

        await _addBatchToQueue.ProcessBatch(currentBatch.AddRecords, _config.ParticipantManagementTopic);
        await _addBatchToQueue.ProcessBatch(currentBatch.UpdateRecords, _config.ParticipantManagementTopic);

        foreach (var updateRecords in currentBatch.DeleteRecords)
        {
            await RemoveParticipant(updateRecords, name);
        }
        // this used to release memory from being used
        currentBatch = null;
    }

    private async Task<bool> UpdateOldDemographicRecord(BasicParticipantCsvRecord basicParticipantCsvRecord, string name)
    {
        try
        {
            long nhsNumber;
            if (!long.TryParse(basicParticipantCsvRecord.Participant.NhsNumber, out nhsNumber))
            {
                throw new FormatException("Unable to parse NHS Number");
            }

            var participant = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumber);

            if (participant == null)
            {
                _logger.LogWarning("The participant could not be found, when trying to update old Participant");
                return false;
            }

            basicParticipantCsvRecord.Participant.RecordInsertDateTime = participant.RecordInsertDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
            var participantForUpdate = basicParticipantCsvRecord.Participant.ToParticipantDemographic();

            participantForUpdate.RecordUpdateDateTime = DateTime.UtcNow;
            participantForUpdate.ParticipantId = participant.ParticipantId;


            var updated = await _participantDemographic.Update(participantForUpdate);
            if (updated)
            {
                _logger.LogInformation("updating old Demographic record was successful");
                return updated;
            }

            _logger.LogError("updating old Demographic record was not successful");
            throw new InvalidOperationException("updating old Demographic record was not successful");
        }
        catch (Exception ex)
        {
            var errorDescription = $"Update participant function failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
            _logger.LogError(ex, errorDescription);
            await CreateError(basicParticipantCsvRecord.Participant, name, errorDescription);
        }
        return false;
    }

    // TODO: refactor now that it all uses one queue
    private async Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
    {
        var allowDeleteRecords = _config.AllowDeleteRecords;
        try
        {
            if (allowDeleteRecords)
            {
                _logger.LogInformation("AllowDeleteRecords flag is true, delete record sent to RemoveParticipant function.");
                await _addBatchToQueue.AddMessage(basicParticipantCsvRecord, _config.ParticipantManagementTopic);
            }
            else
            {
                await _exceptionHandler.CreateDeletedRecordException(basicParticipantCsvRecord);
                _logger.LogInformation("AllowDeleteRecords flag is false, exception raised for delete record.");
            }
        }
        catch (Exception ex)
        {
            var errorDescription = $"Remove participant function failed. Message: {ex.Message} Stack Trace: {ex.StackTrace}";
            _logger.LogError(ex, errorDescription);
            await CreateError(basicParticipantCsvRecord.Participant, filename, errorDescription);
        }
    }

    private async Task CreateError(Participant participant, string filename, string errorDescription)
    {
        try
        {
            _logger.LogError("Cannot parse record type with action: {ParticipantRecordType}", participant.RecordType);
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, filename, errorDescription, "", JsonSerializer.Serialize(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLog(ex, participant, filename);
        }
    }
}
