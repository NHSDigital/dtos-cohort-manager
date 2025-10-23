namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

public class ProcessCaasFile : IProcessCaasFile
{
    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;
    private readonly ReceiveCaasFileConfig _config;


    public ProcessCaasFile(
        ILogger<ProcessCaasFile> logger,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IExceptionHandler exceptionHandler,
        IDataServiceClient<ParticipantDemographic> participantDemographic,
        IRecordsProcessedTracker recordsProcessedTracker,
        IValidateDates validateDates,
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
        _config = receiveCaasFileConfig.Value;
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

        await AddBatchToQueue(currentBatch, name);
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

        // Upsert demographic record immediately (no batching)
        await UpdateOldDemographicRecord(basicParticipantCsvRecord, fileName);

        // Add to Service Bus queues based on record type
        switch (participant.RecordType?.Trim())
        {
            case Actions.New:
                currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                break;
            case Actions.Amended:
                currentBatch.UpdateRecords.Enqueue(basicParticipantCsvRecord);
                break;
            case Actions.Removed:
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

            // Use Upsert instead of separate Get + Update
            // This handles both insert and update atomically at the database level
            var participantForUpsert = basicParticipantCsvRecord.Participant.ToParticipantDemographic();
            participantForUpsert.RecordUpdateDateTime = DateTime.UtcNow;

            // Note: For new records, RecordInsertDateTime will be set by the database
            // For existing records, it will be preserved
            // The ParticipantId will be set automatically by the database for new records

            var upserted = await _participantDemographic.Upsert(participantForUpsert);
            if (upserted)
            {
                _logger.LogInformation("Upsert of Demographic record was successful for NHS Number: {NhsNumber}", nhsNumber);
                return true;
            }

            _logger.LogError("Upsert of Demographic record was not successful for NHS Number: {NhsNumber}", nhsNumber);
            throw new InvalidOperationException($"Upsert of Demographic record was not successful for NHS Number: {nhsNumber}");
        }
        catch (Exception ex)
        {
            var errorDescription = $"Upsert participant function failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
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
