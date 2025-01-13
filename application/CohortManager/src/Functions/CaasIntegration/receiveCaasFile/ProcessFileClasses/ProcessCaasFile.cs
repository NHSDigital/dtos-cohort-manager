namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Azure.Storage.Blobs;
using System.Threading.Tasks.Dataflow;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using receiveCaasFile;

public class ProcessCaasFile : IProcessCaasFile
{

    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IStateStore _stateStore;
    private readonly ICallFunction _callFunction;

    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;

    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IExceptionHandler _handleException;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;

    private readonly IRecordsProcessedTracker _recordsProcessTracker;

    private readonly IValidateDates _validateDates;
    private const int BaseDelayMilliseconds = 2000;

    public ProcessCaasFile(ILogger<ProcessCaasFile> logger, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData,
     IExceptionHandler handleException, IAddBatchToQueue addBatchToQueue, IReceiveCaasFileHelper receiveCaasFileHelper, IExceptionHandler exceptionHandler
     , IRecordsProcessedTracker recordsProcessedTracker, IValidateDates validateDates
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
    }

/// <summary>
/// Processes a file containing participant data with retry logic in case of failures.
/// Tracks the processing state to ensure records are not reprocessed unnecessarily.
/// </summary>
/// <param name="filePath">The path to the file being processed.</param>
/// <param name="values">The list of participant records to be processed.</param>
/// <param name="options">Options for parallel processing.</param>
/// <param name="screeningService">The service used for participant screening and validation.</param>
/// <param name="fileName">The name of the file being processed.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
/// <exception cref="Exception">
/// Thrown when the maximum number of retry attempts is reached and the file cannot be processed successfully.
/// </exception>
    public async Task ProcessRecordsWithRetry(
        string filePath,
        List<ParticipantsParquetMap> values,
        ParallelOptions options,
        ScreeningService screeningService,
        string fileName)
    {
        const int MaxRetryAttempts  = 3;
        int retryCount = 0;
        bool isSuccessful = false;
        int lastProcessedIndex = await _stateStore.GetLastProcessedRecordIndex(fileName) ?? 0;

        while (retryCount < MaxRetryAttempts && !isSuccessful)
        {
            try
            {
                _logger.LogInformation(
                    "Starting to process file {FilePath}, attempt {RetryAttempt}, resuming from index {LastProcessedIndex}",
                    filePath, retryCount + 1, lastProcessedIndex);

                var remainingRecords = values.Skip(lastProcessedIndex).ToList();

                foreach (var record in remainingRecords)
                {
                    try
                    {
                    var participant = await _receiveCaasFileHelper.MapParticipant(record, screeningService.ScreeningId, screeningService.ScreeningName, fileName);

                        if (participant == null)
                        {
                            _logger.LogWarning("Skipping record as participant mapping failed.");
                            continue;
                        }

                        if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
                        {
                            await _exceptionHandler.CreateSystemExceptionLog(
                                new Exception($"Invalid NHS Number for participant {participant.ParticipantId}"),
                                participant,
                                fileName);
                            continue;
                        }

                        if (!_validateDates.ValidateAllDates(participant))
                        {
                            await _exceptionHandler.CreateSystemExceptionLog(
                                new Exception($"Invalid effective date for participant {participant.ParticipantId}"),
                                participant,
                                fileName);
                            continue;
                        }

                        _logger.LogInformation("Successfully processed record for participant {ParticipantId}.", participant.ParticipantId);

                        int currentIndex = values.IndexOf(record) + 1;
                        await _stateStore.UpdateLastProcessedRecordIndex(fileName, currentIndex);
                    }
                    catch (Exception recordEx)
                    {

                        _logger.LogError(recordEx, "Error processing record for file {FileName}.", fileName);
                    }
                }

                _logger.LogInformation("File {FilePath} processed successfully.", filePath);
                isSuccessful = true;
            }
            catch (Exception batchEx)
            {
                retryCount++;
                _logger.LogError(
                    batchEx,
                    "Error occurred while processing file {FilePath}. Attempt {RetryAttempt} of {MaxRetries}",
                    filePath, retryCount, MaxRetryAttempts);

                if (retryCount >= MaxRetryAttempts)
                {
                    _logger.LogWarning("Max retry attempts reached for file {FileName}. Handling failure.", fileName);
                    await HandleFileFailure(filePath, fileName, batchEx.Message);
                    break;
                }

                int retryDelay = BaseDelayMilliseconds * (int)Math.Pow(2, retryCount - 1);
                _logger.LogInformation("Retrying in {RetryDelay} milliseconds...", retryDelay);
                await Task.Delay(retryDelay);
            }
        }

        if (isSuccessful)
        {
            await _stateStore.ClearProcessingState(fileName);
        }
    }
    private async Task HandleFileFailure(string filePath, string fileName, string errorMessage, Participant participant = null)
    {
        try
        {
            byte[] fileData = await File.ReadAllBytesAsync(filePath);

            var blobFile = new BlobFile(fileData, fileName);

            bool isUploaded = await _blobStorageHelper.UploadFileToBlobStorage(
                connectionString: Environment.GetEnvironmentVariable("AzureBlobConnectionString"),
                containerName: "FailedFilesContainer",
                blobFile: blobFile,
                overwrite: true);

            if (isUploaded)
            {
                _logger.LogInformation("File {FileName} successfully moved to blob storage after max retries.", fileName);
            }
            else
            {
                _logger.LogWarning("Failed to move file {FileName} to blob storage after max retries.", fileName);
            }

        await _exceptionHandler.CreateSystemExceptionLog(
            new Exception(errorMessage),
            participant,
            fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling file failure for FilePath: {FilePath}, FileName: {FileName}", filePath, fileName);
            await _exceptionHandler.CreateSystemExceptionLog(
                ex,
                participant,
                fileName);
            throw;
        }
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
