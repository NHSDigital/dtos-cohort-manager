namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Data.Database;
using System;
using System.Collections.Generic;
using System.IO;
using ParquetSharp.RowOriented;
using System.Threading.Tasks;
using Common.Interfaces;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly IScreeningServiceData _screeningServiceData;

    public ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, IReceiveCaasFileHelper receiveCaasFileHelper, IScreeningServiceData screeningServiceData)
    {
        _logger = logger;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _screeningServiceData = screeningServiceData;
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

            var numberOfRecords = fileNameParser.FileCount();
            if (numberOfRecords == null || numberOfRecords == 0)
            {
                string errorMessage = "File name is invalid. File name: " + name;
                _logger.LogError(errorMessage);
                await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, errorMessage);
                return;
            }
            _logger.LogInformation($"Number of records expected {numberOfRecords}");

            var badRecords = new Dictionary<int, string>();
            Cohort cohort = new()
            {
                FileName = name
            };

            var chunks = new List<Cohort>();
            var rowNumber = 0;
            var batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchSize"));

            downloadFilePath = Path.Combine(Path.GetTempPath(), name);

            _logger.LogInformation("Downloading file from the blob, file: {Name}.", name);
            await using (var fileStream = File.Create(downloadFilePath))
            {
                await blobStream.CopyToAsync(fileStream);
            }
            var screeningService = GetScreeningService(fileNameParser);

            using (var rowReader = ParquetFile.CreateRowReader<ParticipantsParquetMap>(downloadFilePath))
            {
                /* A Parquet file is divided into one or more row groups. Each row group contains a specific number of rows.*/
                for (var i = 0; i < rowReader.FileMetaData.NumRowGroups; ++i)
                {
                    var values = rowReader.ReadRows(i);
                    foreach (var rec in values)
                    {
                        rowNumber++;

                        var participant = new Participant
                        {
                            ParticipantUUID = Guid.NewGuid(),
                            ScreeningId = screeningService.ScreeningId,
                            ScreeningName = screeningService.ScreeningName

                        };
                        participant = await _receiveCaasFileHelper.MapParticipant(rec, participant, name, rowNumber);

                        if (participant is null)
                        {
                            chunks.Clear();
                            cohort.Participants.Clear();
                            _logger.LogError("Invalid data in the file: {Name}", name);
                            return;
                        }
                        cohort.Participants.Add(participant);

                        if (cohort.Participants.Count == batchSize)
                        {
                            chunks.Add(cohort);
                            cohort.Participants.Clear();
                        }
                    }
                }
            }

            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);

            if (rowNumber != numberOfRecords)
            {
                _logger.LogError("File name record count not equal to actual record count. File name count: {NumberOfRecords} | Actual count: {RowNumber}", numberOfRecords, rowNumber);
                await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, "N/A");
                return;
            }

            await _receiveCaasFileHelper.SerializeParquetFile(chunks, cohort, name, rowNumber);
            _logger.LogInformation("All rows processed for file named {Name}.", name);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
            await _receiveCaasFileHelper.InsertValidationErrorIntoDatabase(name, "N/A");
            return;
        }
        finally
        {
            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
        }
    }

    private ScreeningService GetScreeningService(FileNameParser fileNameParser)
    {
        var screeningAcronym = fileNameParser.GetScreeningService();
        _logger.LogInformation("screening Acronym {screeningAcronym}", screeningAcronym);
        return _screeningServiceData.GetScreeningServiceByAcronym(screeningAcronym);
    }

}
