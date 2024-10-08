namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Model;
using Common;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Data.Database;
using System.Reflection.PortableExecutable;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IScreeningServiceData _screeningServiceData;



    public ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, ICallFunction callFunction, IScreeningServiceData screeningServiceData)
    {
        _logger = logger;
        _callFunction = callFunction;
        _screeningServiceData = screeningServiceData;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream stream, string name)
    {
        try
        {
            _logger.LogInformation("loading file from blob {name}", name);

            FileNameParser fileNameParser = new FileNameParser(name);
            if (!fileNameParser.IsValid)
            {
                _logger.LogError("File name or file extension is invalid. Not in format BSS_ccyymmddhhmmss_n8.csv. file Name: " + name);
                await InsertValidationErrorIntoDatabase(name, "N/A");
                return;
            }

            var numberOfRecords = fileNameParser.FileCount();
            if (numberOfRecords == null)
            {
                _logger.LogError("File name is invalid. File name: " + name);
                await InsertValidationErrorIntoDatabase(name, "N/A");
                return;
            }
            _logger.LogInformation($"Number of records expected {numberOfRecords}");

            var badRecords = new Dictionary<int, string>();
            Cohort cohort = new()
            {
                FileName = name
            };
            var rowNumber = 0;
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                Delimiter = ",",
                HeaderValidated = null
            };
            try
            {
                using var blobStreamReader = new StreamReader(stream);
                using var csv = new CsvReader(blobStreamReader, config);
                csv.Context.RegisterClassMap<ParticipantMap>();
                var records = csv.GetRecords<Participant>();
                var screeningService = GetScreeningService(fileNameParser);

                _logger.LogInformation("screeningService {screeningService}", screeningService.ScreeningName);

                foreach (var participant in records)
                {
                    rowNumber++;
                    try
                    {
                        if (participant != null)
                        {
                            participant.ScreeningId = screeningService.ScreeningId;
                            participant.ScreeningName = screeningService.ScreeningName;
                            cohort.Participants.Add(participant);
                        }
                    }
                    catch (Exception ex)
                    {
                        badRecords.Add(rowNumber, csv.Context.Parser.RawRecord);
                        _logger.LogError("Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", rowNumber, ex.Message, ex.StackTrace);
                        await InsertValidationErrorIntoDatabase(name, JsonSerializer.Serialize(participant));
                    }
                }

                if (rowNumber != numberOfRecords)
                {
                    _logger.LogError("File name record count not equal to actual record count. File name count: " + name + "| Actual count: " + rowNumber);
                    await InsertValidationErrorIntoDatabase(name, "N/A");
                    return;
                }
                if (rowNumber == 0)
                {
                    _logger.LogError("File contains no records. File name:" + name);
                    await InsertValidationErrorIntoDatabase(name, "N/A");
                    return;
                }
            }
            catch (Exception ex) when (ex is HeaderValidationException || ex is CsvHelperException || ex is FileFormatException)
            {
                _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name, "N/A");
                return;
            }
            try
            {
                if (cohort.Participants.Count > 0)
                {
                    var json = JsonSerializer.Serialize(cohort);
                    await _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    _logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                }
                if (badRecords.Count > 0 || cohort.Participants.Count == 0)
                {
                    _logger.LogError("Failed to create {BadRecordsCount} Objects", badRecords.Count);
                    _logger.LogError("All failed Records - {BadRecords}", badRecords);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Message:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name, "N/A");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name, "N/A");
            return;
        }
    }

    private async Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord)
    {
        var json = JsonSerializer.Serialize<Model.ValidationException>(new Model.ValidationException()
        {
            FileName = fileName,
            ErrorRecord = errorRecord
        });

        var result = await _callFunction.SendPost(Environment.GetEnvironmentVariable("FileValidationURL"), json);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("An error occurred while saving or moving the failed file {fileName}.", fileName);
        }
        _logger.LogInformation("File failed checks and has been moved to the poison blob storage");

    }

    private ScreeningService GetScreeningService(FileNameParser fileNameParser)
    {
        var screeningAcronym = fileNameParser.GetScreeningService();
        _logger.LogInformation("screening Acronym {screeningAcronym}", screeningAcronym);
        return _screeningServiceData.GetScreeningServiceByAcronym(screeningAcronym);
    }
}
