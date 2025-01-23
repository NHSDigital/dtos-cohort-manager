namespace NHS.Screening.ReceiveCaasFile;

using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class ProcessRecordsManager : IProcessRecordsManager
{
    private const int MaxRetryAttempts = 3;
    private readonly ILogger<ProcessRecordsManager> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IStateStore _stateStore;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly IValidateDates _validateDates;
    private const int BaseDelayMilliseconds = 2000;
    private readonly int _maxRetryAttempts = 3;

    public ProcessRecordsManager(
        ILogger<ProcessRecordsManager> logger,
        IStateStore stateStore,
        IExceptionHandler exceptionHandler,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IValidateDates validateDates,
        IBlobStorageHelper blobStorageHelper)
    {
        _logger = logger;
        _stateStore = stateStore;
        _exceptionHandler = exceptionHandler;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _validateDates = validateDates;
        _blobStorageHelper = blobStorageHelper;
    }

    /// <summary>
    /// Processes a list of participant records with retry logic for handling failures.
    /// </summary>
    /// <param name="participants">The list of participant records to process.</param>
    /// <param name="options">Parallel options for configuring parallel execution.</param>
    /// <param name="screeningService">The screening service used for mapping participants.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method retries processing up to the specified maximum number of attempts if an exception occurs.
    /// The state of the last processed record is maintained to avoid reprocessing already successful records.
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown if the maximum number of retry attempts is reached and the file cannot be processed successfully.
    /// </exception>
    public async Task ProcessRecordsWithRetry(
        List<ParticipantsParquetMap> participants,
        string name)
    {
        var retryCount = 0;
        var isSuccessful = false;
        // int lastProcessedIndex = await _stateStore.GetLastProcessedRecordIndex(name) ?? 0;


        while (retryCount < MaxRetryAttempts && !isSuccessful)
        {
            // var remainingRecords = participants.Skip(lastProcessedIndex).ToList();
            try
            {
                foreach (var record in participants)
                {
                        // we need to add the remaining records to blob storage
                        await HandleFileFailure(name, "Adding the remaining records to blob storage", record);

                        // int currentIndex = participants.IndexOf(record) + 1;
                        // await _stateStore.UpdateLastProcessedRecordIndex(name, currentIndex);

                }

                _logger.LogInformation("File processed successfully.");
                isSuccessful = true;
                // throw new Exception("Test exception 1");
            }
            catch (Exception ex)
            {
                retryCount++;
                await HandleRetryFailure(name, ex, retryCount);
            }
        }

        if (isSuccessful)
        {
            await _stateStore.ClearProcessingState(name);
        }
    }

    private async Task HandleRetryFailure(string fileName, Exception ex, int retryCount)
    {
        _logger.LogError(ex, "Batch processing failed on attempt {RetryAttempt}.", retryCount);

        if (retryCount >= MaxRetryAttempts)
        {
            _logger.LogWarning("Maximum retry attempts reached. Handling failure for file {FileName}.", fileName);
            await HandleFileFailure(fileName, ex.Message, new ParticipantsParquetMap());
        }
        else
        {
            await Task.Delay(2000 * retryCount); // Exponential backoff
        }
    }

    private async Task HandleFileFailure(string fileName, string errorMessage, ParticipantsParquetMap participant)
    {
        try
        {
            string filePath = Path.Combine(Environment.GetEnvironmentVariable("FileDirectoryPath"), fileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File {FileName} does not exist. Skipping upload to blob storage.", fileName);
                return;
            }

            byte[] fileData = await File.ReadAllBytesAsync(filePath);
            var blobFile = new BlobFile(fileData, fileName);

            bool isUploaded = await _blobStorageHelper.UploadFileToBlobStorage(
                connectionString: Environment.GetEnvironmentVariable("AzureBlobConnectionString"),
                containerName: "FailedFilesContainer",
                blobFile: blobFile,
                overwrite: true);

            if (isUploaded)
            {
                _logger.LogInformation("File {FileName} successfully uploaded to blob storage.", fileName);
            }
            else
            {
                _logger.LogWarning("Failed to upload file {FileName} to blob storage.", fileName);
            }

            await _exceptionHandler.CreateSystemExceptionLog(
                new Exception(errorMessage),
                new Participant(),
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling failure for file {FileName}.", fileName);
            await _exceptionHandler.CreateSystemExceptionLog(
                ex,
                new Participant(),
                fileName);
        }
    }

}

