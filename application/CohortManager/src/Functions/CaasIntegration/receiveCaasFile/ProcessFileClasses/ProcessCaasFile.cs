namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using receiveCaasFile;

public class ProcessCaasFile : IProcessCaasFile
{

    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IExceptionHandler _handleException;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;

    private IStateStore _stateStore;

    public ProcessCaasFile(ILogger<ProcessCaasFile> logger, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData,
     IExceptionHandler handleException, IAddBatchToQueue addBatchToQueue, IReceiveCaasFileHelper receiveCaasFileHelper, IExceptionHandler exceptionHandler
     , IRecordsProcessedTracker recordsProcessedTracker, IValidateDates validateDates, IStateStore stateStore
     )
    {
        _logger = logger;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _handleException = handleException;
        _addBatchToQueue = addBatchToQueue;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _exceptionHandler = exceptionHandler;
        _recordsProcessTracker = recordsProcessedTracker;
        _validateDates = validateDates;
        _stateStore = stateStore;
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
                await UpdateParticipant(updateRecords, name);
            }

            foreach (var updateRecords in currentBatch.DeleteRecords)
            {
                await RemoveParticipant(updateRecords, name);
            }
        }
    }

    private async Task UpdateParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string name)
    {
        try
        {
            var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
            if (await _checkDemographic.PostDemographicDataAsync(basicParticipantCsvRecord.participant, Environment.GetEnvironmentVariable("DemographicURI")))
            {
                await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSUpdateParticipant"), json);
            }
            _logger.LogInformation("Called update participant");


            //consider putting this code in a method of its own as its repeated here and in add batch the queue
            var recordsNotProcessed = _stateStore.GetListOfAllValues();
            var recordToRemove = _stateStore.GetListOfAllValues().Where(x => x.NhsNumber.ToString() == basicParticipantCsvRecord.participant.NhsNumber).ToList().FirstOrDefault();
            recordsNotProcessed.Remove(recordToRemove);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(basicParticipantCsvRecord.participant, name);
        }
    }

    private async Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
    {
        try
        {
            await _handleException.CreateDeletedRecordException(basicParticipantCsvRecord);
            _logger.LogInformation("Logged Exception for Deleted Record");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(basicParticipantCsvRecord.participant, filename);
        }
    }

    private async Task CreateError(Participant participant, string filename)
    {
        try
        {
            _logger.LogError("Cannot parse record type with action: {ParticipantRecordType}", participant.RecordType);
            var errorDescription = $"a record has failed to process with the NHS Number : {participant.NhsNumber} because the of an incorrect record type";
            await _handleException.CreateRecordValidationExceptionLog(participant.NhsNumber, filename, errorDescription, "", JsonSerializer.Serialize(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            _handleException.CreateSystemExceptionLog(ex, participant, filename);
        }
    }

}
