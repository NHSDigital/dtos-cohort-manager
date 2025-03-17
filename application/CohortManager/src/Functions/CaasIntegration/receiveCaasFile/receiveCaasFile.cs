namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using System;
using System.IO;
using ParquetSharp.RowOriented;
using System.Threading.Tasks;
using Common.Interfaces;
using DataServices.Client;
using Common;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly IProcessCaasFile _processCaasFile;
    private readonly IDataServiceClient<ScreeningLkp> _screeningLkpClient;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IExceptionHandler _exceptionHandler;

    public ReceiveCaasFile(
        ILogger<ReceiveCaasFile> logger,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IProcessCaasFile processCaasFile,
        IDataServiceClient<ScreeningLkp> screeningLkpClient
        )
    {
        _logger = logger;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _processCaasFile = processCaasFile;
        _screeningLkpClient = screeningLkpClient;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string fileName)
    {
        var downloadFilePath = string.Empty;
        // for larger batches use size of 5000 - this works the best
        int.TryParse(Environment.GetEnvironmentVariable("BatchSize"), out var BatchSize);
        try
        {
            FileNameParser fileNameParser = new(fileName);
            if (!fileNameParser.IsValid)
                throw new ArgumentException("File name is invalid, file name: " + fileName);

            var screeningService = await GetScreeningService(fileNameParser);

            downloadFilePath = Path.Combine(Path.GetTempPath(), fileName);

            _logger.LogInformation("Downloading file from the blob, file: {Name}.", fileName);
            await using (var fileStream = File.Create(downloadFilePath))
            {
                await blobStream.CopyToAsync(fileStream);
            }

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            using (var rowReader = ParquetFile.CreateRowReader<ParticipantsParquetMap>(downloadFilePath))
            {
                // A Parquet file is divided into one or more row groups. Each row group contains a specific number of rows.
                for (var i = 0; i < rowReader.FileMetaData.NumRowGroups; ++i)
                {
                    var values = rowReader.ReadRows(i);
                    var listOfAllValues = values.ToList();
                    var allTasks = new List<Task>();

                    //split list of all into N amount of chunks to be processed as batches.
                    var chunks = listOfAllValues.Chunk(BatchSize).ToList();

                    foreach (var chunk in chunks)
                    {
                        var batch = chunk.ToList();
                        allTasks.Add(
                            _processCaasFile.ProcessRecords(batch, options, screeningService, fileName)
                        );
                    }

                    // process each batches
                    Task.WaitAll(allTasks.ToArray());

                    // dispose of all lists and variables from memory because they are no longer needed
                    listOfAllValues.Clear();
                    values.ToList().Clear();
                }
            }

            _logger.LogInformation("All rows processed for file named {Name}. time {Time}", fileName, DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
            await _exceptionHandler.CreateRecordValidationExceptionLog("", fileName, ex.Message, "", requestBody.ErrorRecord);
            await _blobStorageHelper.CopyFileToPoisonAsync(Environment.GetEnvironmentVariable("caasfolder_STORAGE"), fileName, Environment.GetEnvironmentVariable("inboundBlobName"));
        }
        finally
        {
            //We want to release the file from temporary storage no matter what
            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
        }
    }

    /// <summary>
    /// gets the screening service data for a screening work flow
    /// </summary>
    /// <param name="fileNameParser"></param>
    /// <exception cref="ArgumentExcpetion">
    /// Thrown if the screening service could not be found
    /// </exception>
    public async Task<ScreeningLkp> GetScreeningService(FileNameParser fileNameParser)
    {
        var screeningWorkflowId = fileNameParser.GetScreeningService();
        _logger.LogInformation("Screening Acronym {screeningWorkflowId}", screeningWorkflowId);

        ScreeningLkp screeningService = await _screeningLkpClient.GetSingleByFilter(x => x.ScreeningWorkflowId == screeningWorkflowId)
            ?? throw new ArgumentException("Could not get screening service data for screening id: " + screeningWorkflowId);

        return screeningService;
    }
}
