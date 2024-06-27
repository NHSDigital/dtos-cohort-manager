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

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly ICallFunction _callFunction;

    public ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream stream, string name)
    {
        FileExtensionCheck(name);

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
            foreach (var participant in csv.GetRecords<Participant>())
            {
                rowNumber++;
                try
                {
                    if (participant != null)
                    {
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
        }
        catch (Exception ex) when (ex is HeaderValidationException || ex is CsvHelperException)
        {
            _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
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
            _logger.LogError("Unable to call function.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
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
        if (result.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("file failed checks and has been moved to the poison blob storage");
        }
        _logger.LogError("there was a problem saving and or moving the failed file");
    }

    private static void FileExtensionCheck(string name)
    {
        var fileExtension = Path.GetExtension(name).ToLower();
        if (fileExtension != FileFormats.CSV)
        {
            throw new NotSupportedException("Invalid file type. Only CSV files are allowed.");
        };
    }
}
