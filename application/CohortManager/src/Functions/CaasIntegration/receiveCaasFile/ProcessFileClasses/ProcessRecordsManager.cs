namespace NHS.Screening.ReceiveCaasFile;

    using System.Text.Json;
    using Azure.Storage.Blobs;
    using System.Threading.Tasks.Dataflow;
    using Common;
    using Common.Interfaces;
    using Microsoft.Extensions.Logging;
    using Model;
    using receiveCaasFile;

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
            ParallelOptions options,
            ScreeningService screeningService,
            string name)
        {
            int retryCount = 0;
            bool isSuccessful = false;
            int lastProcessedIndex = await _stateStore.GetLastProcessedRecordIndex(name) ?? 0;

            while (retryCount < MaxRetryAttempts && !isSuccessful)
            {
                try
                {
                    _logger.LogInformation(
                        "Starting processing with retry attempt {RetryAttempt}, resuming from index {LastProcessedIndex}.",
                        retryCount + 1, lastProcessedIndex);

                    var remainingRecords = participants.Skip(lastProcessedIndex).ToList();

                    foreach (var record in remainingRecords)
                    {
                        try
                        {
                            var participant = await _receiveCaasFileHelper.MapParticipant(
                                record, screeningService.ScreeningId, screeningService.ScreeningName, name);

                            if (participant == null || !ValidateParticipant(participant, name))
                            {
                                continue;
                            }

                            _logger.LogInformation("Processed participant {ParticipantId}.", participant.ParticipantId);
                            int currentIndex = participants.IndexOf(record) + 1;
                            await _stateStore.UpdateLastProcessedRecordIndex(name, currentIndex);
                        }
                        catch (Exception recordEx)
                        {
                            _logger.LogError(recordEx, "Error processing participant record in file {FileName}.", name);
                        }
                    }

                    _logger.LogInformation("File processed successfully.");
                    isSuccessful = true;
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
                await HandleFileFailure(fileName, ex.Message);
            }
            else
            {
                await Task.Delay(2000 * retryCount); // Exponential backoff
            }
        }
        private async Task HandleFileFailure(string fileName, string errorMessage, Participant participant = null)
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
                    participant,
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling failure for file {FileName}.", fileName);
                await _exceptionHandler.CreateSystemExceptionLog(
                    ex,
                    participant,
                    fileName);
            }
        }
        private bool ValidateParticipant(Participant participant, string fileName)
        {
            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                _exceptionHandler.CreateSystemExceptionLog(
                    new Exception($"Invalid NHS Number for participant {participant.ParticipantId}"),
                    participant,
                    fileName);
                return false;
            }

            if (!_validateDates.ValidateAllDates(participant))
            {
                _exceptionHandler.CreateSystemExceptionLog(
                    new Exception($"Invalid effective date for participant {participant.ParticipantId}"),
                    participant,
                    fileName);
                return false;
            }

            return true;
        }

    }

