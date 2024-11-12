namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Data.Database;
using System;
using System.IO;
using ParquetSharp.RowOriented;
using System.Threading.Tasks;
using Common.Interfaces;
using Common;
using System.Security.Cryptography.X509Certificates;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly IScreeningServiceData _screeningServiceData;
    private readonly IProcessCaasFile _processCaasFile;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;

    private readonly ICheckDemographic _checkDemographic;

    public ReceiveCaasFile(
        ILogger<ReceiveCaasFile> logger,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IScreeningServiceData screeningServiceData,
        IExceptionHandler exceptionHandler, IProcessCaasFile processCaasFile,
        ICreateBasicParticipantData createBasicParticipantData,
        ICheckDemographic checkDemographic
        )
    {
        _logger = logger;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _screeningServiceData = screeningServiceData;
        _exceptionHandler = exceptionHandler;
        _processCaasFile = processCaasFile;
        _createBasicParticipantData = createBasicParticipantData;
        _checkDemographic = checkDemographic;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        var downloadFilePath = string.Empty;
        try
        {
            _logger.LogInformation("loading file from blob {name}", name);

            FileNameParser fileNameParser = new FileNameParser(name);
            if (!fileNameParser.IsValid)
            {
                string errorMessage = "File name is invalid. File name: " + name;
                _logger.LogError(errorMessage);
                await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, errorMessage);
                return;
            }

            downloadFilePath = Path.Combine(Path.GetTempPath(), name);

            _logger.LogInformation("Downloading file from the blob, file: {Name}.", name);
            await using (var fileStream = File.Create(downloadFilePath))
            {
                await blobStream.CopyToAsync(fileStream);
            }

            var screeningService = GetScreeningService(fileNameParser);
            if (string.IsNullOrEmpty(screeningService.ScreeningId) || string.IsNullOrEmpty(screeningService.ScreeningName))
            {
                string errorMessage = "No Screening Service Found for Workflow: " + fileNameParser.GetScreeningService();
                _logger.LogError(errorMessage);
                await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, errorMessage);
                return;
            }
            _logger.LogInformation($"Screening Name: {screeningService.ScreeningName}");

            int rowNumber = 0; ;
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            await _processCaasFile.CreateAddQueueCLient();

            using (var rowReader = ParquetFile.CreateRowReader<ParticipantsParquetMap>(downloadFilePath))
            {
                // A Parquet file is divided into one or more row groups. Each row group contains a specific number of rows.
                for (var i = 0; i < rowReader.FileMetaData.NumRowGroups; ++i)
                {
                    var values = rowReader.ReadRows(i);


                    var listOfAllValues = values.ToList();
                    var countOfRecords = values.Length;

                    if (countOfRecords > 3)
                    {
                        //split list of all into N amount of chunks to be processed as batches
                        var chunkSize = countOfRecords / 5;
                        var chunks = listOfAllValues.Chunk(chunkSize).ToList();

                        var allTasks = new List<Task>();
                        foreach (var chunk in chunks)
                        {
                            var batch = chunk.ToList();
                            allTasks.Add(
                                processRecords(batch, options, screeningService, name)
                            );
                        }

                        // process each batches
                        Task.WaitAll(allTasks.ToArray());
                    }
                    else
                    {
                        await processRecords(listOfAllValues, options, screeningService, name);
                    }
                    // dispose of all lists and variables from memory because they are no longer needed
                    listOfAllValues = null;
                    values = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
            await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, "N/A");
            return;
        }
        finally
        {
            _logger.LogInformation("All rows processed for file named {Name}. time {time}", name, DateTime.Now);
            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
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
    private async Task processRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningService screeningService, string name)
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
                await _exceptionHandler.CreateSystemExceptionLog(new Exception("Invalid NHS Number was passed in for participant {participant} and file {name}"), participant, name);

                return; // skip current participant
            }

            if (!_receiveCaasFileHelper.validateDateTimes(participant))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception("Invalid effective date found in participant data {participant} and file name {name}"), participant, name);
                return; // Skip current participant
            }

            await AddRecordToBatch(participant, currentBatch, name);

        });
        // _logger.LogInformation(currentBatch.AddRecords.Count().ToString());
        await _processCaasFile.AddBatchToQueue(currentBatch, name);
    }

    /// <summary>
    /// adds a given record to the current given batch
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="currentBatch"></param>
    /// <param name="FileName"></param>
    /// <returns></returns>
    private async Task<Batch> AddRecordToBatch(Participant participant, Batch currentBatch, string FileName)
    {
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = FileName,
            participant = participant
        };

        switch (participant.RecordType?.Trim())
        {
            case Actions.New:
                //  we do this check in here because we can't do it in AddBatchToQueue with the rest of the calls
                if (await _checkDemographic.PostDemographicDataAsync(basicParticipantCsvRecord.participant, Environment.GetEnvironmentVariable("DemographicURI")))
                {
                    // we need to wait on this dark to completed here
                    await Task.CompletedTask;
                    currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                }
                break;
            case Actions.Amended:
                currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                break;
            case Actions.Removed:
                currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                break;
            default:
                break;
        }
        return currentBatch;

    }
    /// <summary>
    /// gets the screening service data for a screening work flow
    /// </summary>
    /// <param name="fileNameParser"></param>
    /// <returns></returns>
    private ScreeningService GetScreeningService(FileNameParser fileNameParser)
    {

        var ScreeningWorkflow = fileNameParser.GetScreeningService();
        _logger.LogInformation("screening Acronym {screeningAcronym}", ScreeningWorkflow);
        return _screeningServiceData.GetScreeningServiceByWorkflowId(ScreeningWorkflow);
    }

}
