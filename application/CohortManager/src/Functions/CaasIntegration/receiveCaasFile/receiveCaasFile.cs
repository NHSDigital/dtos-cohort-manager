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
        var badRecords = new Dictionary<int, string>();
        var cohort = new Cohort()
        {
            FileName = name
        };
        var rowNumber = 0;
        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            Delimiter = ",",
            MissingFieldFound = null,
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
        catch (HeaderValidationException ex)
        {
            _logger.LogError("Header validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError("Failure occurred when reading the CSV file.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
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
}
