namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using System;
using System.IO;
using ParquetSharp.RowOriented;
using System.Threading.Tasks;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ReconciliationServiceCore;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly IProcessCaasFile _processCaasFile;
    private readonly IDataServiceClient<ScreeningLkp> _screeningLkpClient;
    private readonly ReceiveCaasFileConfig _config;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IInboundMetricClient _inboundMetricClient;

    public ReceiveCaasFile(
        ILogger<ReceiveCaasFile> logger,
        IProcessCaasFile processCaasFile,
        IDataServiceClient<ScreeningLkp> screeningLkpClient,
        IOptions<ReceiveCaasFileConfig> receiveCaasFileConfig,
        IBlobStorageHelper blobStorageHelper,
        IExceptionHandler exceptionHandler,
        IInboundMetricClient inboundMetricClient
        )
    {
        _logger = logger;
        _processCaasFile = processCaasFile;
        _screeningLkpClient = screeningLkpClient;
        _config = receiveCaasFileConfig.Value;
        _blobStorageHelper = blobStorageHelper;
        _exceptionHandler = exceptionHandler;
        _inboundMetricClient = inboundMetricClient;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        var downloadFilePath = string.Empty;
        string screeningName = string.Empty;
        // for larger batches use size of 5000 - this works the best
        var BatchSize = _config.BatchSize;
        try
        {
            FileNameParser fileNameParser = new(name);
            if (!fileNameParser.IsValid)
                throw new ArgumentException("File name is invalid, file name: " + name);

            var screeningService = await GetScreeningService(fileNameParser);
            screeningName = screeningService.ScreeningName;

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

                await _inboundMetricClient.LogInboundMetric(name, Convert.ToInt32(rowReader.FileMetaData.NumRows));

            }
            _logger.LogInformation("All rows processed for file named {Name}. time {Time}", name, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was a system exception in receive-caas-file");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, "", name, screeningName, "");
            await _blobStorageHelper.CopyFileToPoisonAsync(_config.caasfolder_STORAGE, name, _config.inboundBlobName);
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
