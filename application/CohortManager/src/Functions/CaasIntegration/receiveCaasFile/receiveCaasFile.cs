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
            if (!FileNameAndFileExtensionIsValid(name))
            {
                _logger.LogError("File name or file extension is invalid. Not in format BSS_ccyymmddhhmmss_n8.csv. file Name: " + name);
                await InsertValidationErrorIntoDatabase(name);
                return;
            }

            var numberOfRecords = await GetNumberOfRecordsFromFileName(name);
            if (numberOfRecords == null)
            {
                return;
            }

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
                var screeningService = GetScreeningService(name);

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
                        await InsertValidationErrorIntoDatabase(name);
                    }
                }

                if (rowNumber != numberOfRecords)
                {
                    _logger.LogError("File name record count not equal to actual record count. File name count: " + name + "| Actual count: " + rowNumber);
                    await InsertValidationErrorIntoDatabase(name);
                    return;
                }
                if (rowNumber == 0)
                {
                    _logger.LogError("File contains no records. File name:" + name);
                    await InsertValidationErrorIntoDatabase(name);
                    return;
                }
            }
            catch (Exception ex) when (ex is HeaderValidationException || ex is CsvHelperException || ex is FileFormatException)
            {
                _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
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
                await InsertValidationErrorIntoDatabase(name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
            return;
        }
    }

    private async Task InsertValidationErrorIntoDatabase(string fileName)
    {
        var json = JsonSerializer.Serialize<Model.ValidationException>(new Model.ValidationException()
        {
            RuleId = 1,
            FileName = fileName
        });

        var result = await _callFunction.SendPost(Environment.GetEnvironmentVariable("FileValidationURL"), json);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("An error occurred while saving or moving the failed file {fileName}.", fileName);
        }
        _logger.LogInformation("File failed checks and has been moved to the poison blob storage");

    }

    private static bool FileNameAndFileExtensionIsValid(string name)
    {
        /* for file format BSS_ccyymmddhhmmss_n8.csv
        '^\w{1,}_' Matches the screening acronym, it could be anything before the first underscore
        '\d{14}' Matches exactly 14 digits, representing ccyymmddhhmmss
        '_n' Matches the literal _n
        '([1-9]\d*|0)' Matches any number with no leading zeros OR The number 0.
        '\.csv$' matches .csv at the end of the string */
        var match = Regex.Match(name, @"^\w{1,}_\d{14}_n([1-9]\d*|0)\.csv$", RegexOptions.IgnoreCase);
        return match.Success;
    }

    private async Task<int?> GetNumberOfRecordsFromFileName(string name)
    {
        var str = name.Remove(name.IndexOf('.'));
        var numberOfRecords = (str.Split('_')[2]).Substring(1);

        if (Int32.TryParse(numberOfRecords, out int n))
        {
            return n;
        }
        else
        {
            _logger.LogError("File name is invalid. File name: " + name);
            await InsertValidationErrorIntoDatabase(name);
            return null;
        }
    }

    private ScreeningService GetScreeningService(string name)
    {
        var screeningAcronym = name.Split('_')[0];
        _logger.LogInformation("screening Acronym {screeningAcronym}", screeningAcronym);
        return _screeningServiceData.GetScreeningServiceByAcronym(screeningAcronym);
    }
}
