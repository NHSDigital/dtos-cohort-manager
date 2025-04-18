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
using Microsoft.Extensions.Options;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly IProcessCaasFile _processCaasFile;
    private readonly IDataServiceClient<ScreeningLkp> _screeningLkpClient;
    private readonly ReceiveCaasFileConfig _config;

    public ReceiveCaasFile(
        ILogger<ReceiveCaasFile> logger,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IProcessCaasFile processCaasFile,
        IDataServiceClient<ScreeningLkp> screeningLkpClient,
        IOptions<ReceiveCaasFileConfig> receiveCaasFileConfig
        )
    {
        _logger = logger;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _processCaasFile = processCaasFile;
        _screeningLkpClient = screeningLkpClient;
        _config = receiveCaasFileConfig.Value;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        var ErrorOccurred = false;
        var downloadFilePath = string.Empty;
        // for larger batches use size of 5000 - this works the best
        var BatchSize = _config.BatchSize;
        try
        {
            FileNameParser fileNameParser = new FileNameParser(name);
            var fileNameErrorMessage = "File name is invalid. File name: " + name;
            if (!await _receiveCaasFileHelper.CheckFileName(name, fileNameParser, fileNameErrorMessage))
            {
                _logger.LogError(fileNameErrorMessage);
                ErrorOccurred = true;
                return;
            }

            var screeningService = await GetScreeningService(name, fileNameParser);
            if (string.IsNullOrWhiteSpace(screeningService.ScreeningName) || string.IsNullOrWhiteSpace(screeningService.ScreeningId))
            {
                _logger.LogError("The Screening id or screening name was null or empty");
                ErrorOccurred = true;
                return;
            }

            downloadFilePath = Path.Combine(Path.GetTempPath(), name);

            _logger.LogInformation("Downloading file from the blob, file: {Name}.", name);
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
                            _processCaasFile.ProcessRecords(batch, options, screeningService, name)
                        );
                    }

                    // process each batches
                    Task.WaitAll(allTasks.ToArray());

                    // dispose of all lists and variables from memory because they are no longer needed
                    listOfAllValues.Clear();
                    values.ToList().Clear();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
            await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, "N/A");
        }
        finally
        {
            //We do not want to log here that we have processed all rows as this might be mis leading when looking in the logs in azure
            if (!ErrorOccurred)
            {
                _logger.LogInformation("All rows processed for file named {Name}. time {Time}", name, DateTime.Now);
            }
            //We want to release the file from temporary storage no matter what
            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
        }
    }

    public async Task<ScreeningService> GetScreeningService(string name, FileNameParser fileNameParser)
    {
        // get screening service name and id
        var screeningService = await GetScreeningService(fileNameParser);
        if (string.IsNullOrEmpty(screeningService.ScreeningId) || string.IsNullOrEmpty(screeningService.ScreeningName))
        {
            string errorMessage = "No Screening Service Found for Workflow: " + fileNameParser.GetScreeningService();
            _logger.LogError(errorMessage);
            await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, errorMessage);

            return new ScreeningService();
        }
        return screeningService;
    }

    /// <summary>
    /// gets the screening service data for a screening work flow
    /// </summary>
    /// <param name="fileNameParser"></param>
    /// <returns></returns>
    public async Task<ScreeningService> GetScreeningService(FileNameParser fileNameParser)
    {
        var screeningWorkflowId = fileNameParser.GetScreeningService();
        _logger.LogInformation("Screening Acronym {screeningWorkflowId}", screeningWorkflowId);
        var res = await _screeningLkpClient.GetSingleByFilter(x => x.ScreeningWorkflowId == screeningWorkflowId);
        ScreeningService screeningWorkflow = new ScreeningService
        {
            ScreeningName = res?.ScreeningName,
            ScreeningId = res?.ScreeningId.ToString(),
            ScreeningWorkflowId = res?.ScreeningWorkflowId
        };
        return screeningWorkflow;
    }
}
