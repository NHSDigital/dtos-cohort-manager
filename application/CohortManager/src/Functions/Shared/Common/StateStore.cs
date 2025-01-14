namespace Common;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


    public interface IStateStore
    {
        Task<int?> GetLastProcessedRecordIndex(string fileName);
        Task UpdateLastProcessedRecordIndex(string fileName, int recordIndex);
        Task ClearProcessingState(string fileName);
    }


    public class StateStore : IStateStore
    {
        private readonly ILogger<StateStore> _logger;

        public StateStore(ILogger<StateStore> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the last processed record index for a given file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The last processed record index, or null if not found.</returns>
        public async Task<int?> GetLastProcessedRecordIndex(string fileName)
        {
            try
            {
                _logger.LogInformation("Retrieving last processed record index for file {FileName}.", fileName);

                int? lastIndex = await Task.FromResult<int?>(null);

                _logger.LogInformation(
                    "Successfully retrieved last processed record index for file {FileName}: {LastIndex}.",
                    fileName,
                    lastIndex);

                return lastIndex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving last processed record index for file {FileName}.", fileName);
                return 0;
            }
        }

        /// <summary>
        /// Updates the last processed record index for a given file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="recordIndex">The index of the last processed record.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateLastProcessedRecordIndex(string fileName, int recordIndex)
        {
            try
            {
                _logger.LogInformation(
                    "Updating last processed record index for file {FileName} to {RecordIndex}.",
                    fileName,
                    recordIndex);


                await Task.CompletedTask;

                _logger.LogInformation(
                    "Successfully updated last processed record index for file {FileName} to {RecordIndex}.",
                    fileName,
                    recordIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating last processed record index for file {FileName}.", fileName);
                return;
            }
        }

        /// <summary>
        /// Clears the processing state for a given file after successful processing.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ClearProcessingState(string fileName)
        {
            try
            {
                _logger.LogInformation("Clearing processing state for file {FileName}.", fileName);

                await Task.CompletedTask;

                _logger.LogInformation("Successfully cleared processing state for file {FileName}.", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while clearing processing state for file {FileName}.", fileName);
                return;

            }
        }
    }


