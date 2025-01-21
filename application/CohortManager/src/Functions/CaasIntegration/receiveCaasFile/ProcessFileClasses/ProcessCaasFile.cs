namespace NHS.Screening.ReceiveCaasFile;

using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class ProcessCaasFile : IProcessCaasFile
{
    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;
    private readonly IProcessRecord _processRecord;

    public ProcessCaasFile(ILogger<ProcessCaasFile> logger, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue, IReceiveCaasFileHelper receiveCaasFileHelper, IExceptionHandler exceptionHandler,
        IRecordsProcessedTracker recordsProcessedTracker, IValidateDates validateDates, IProcessRecord processRecord
    )
    {
        _logger = logger;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _exceptionHandler = exceptionHandler;
        _recordsProcessTracker = recordsProcessedTracker;
        _validateDates = validateDates;
        _processRecord = processRecord;
    }

    /// <summary>
    /// process a given batch and send it the queue
    /// </summary>
    /// <param name="values"></param>
    /// <param name="options"></param>
    /// <param name="screeningService"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningService screeningService, string name)
    {
        var currentBatch = new Batch();
        await Parallel.ForEachAsync(values, options, async (rec, cancellationToken) =>
        {
            var participant = await _receiveCaasFileHelper.MapParticipant(rec, screeningService.ScreeningId, screeningService.ScreeningName, name);

            if (participant == null)
            {
                return;
            }

            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Invalid NHS Number was passed in for participant {participant} and file {name}"), participant, name);

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
    private async Task<Batch> AddRecordToBatch(Participant participant, Batch currentBatch, string fileName)
    {
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = fileName,
            participant = participant
        };

        switch (participant.RecordType?.Trim())
        {
            case Actions.New:
                //  we do this check in here because we can't do it in AddBatchToQueue with the rest of the calls
                if (await _checkDemographic.PostDemographicDataAsync(basicParticipantCsvRecord.participant, Environment.GetEnvironmentVariable("DemographicURI")))
                {
                    currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                }
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
        return currentBatch;

    }

    private async Task AddBatchToQueue(Batch currentBatch, string name)
    {
        _logger.LogInformation("sending {count} records to queue", currentBatch.AddRecords.Count);
        await _addBatchToQueue.ProcessBatch(currentBatch);

        if (currentBatch.UpdateRecords.LongCount() > 0 || currentBatch.DeleteRecords.LongCount() > 0)
        {
            foreach (var updateRecords in currentBatch.UpdateRecords)
            {
                await _processRecord.UpdateParticipant(updateRecords, name);
            }

            foreach (var updateRecords in currentBatch.DeleteRecords)
            {
                await _processRecord.RemoveParticipant(updateRecords, name);
            }
        }
    }
}
