namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Model;
using Common;
using System.Net;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Data.Database;
using Model.Enums;
using ParquetSharp.RowOriented;
using System.Threading.Tasks;

public class ReceiveCaasFile
{
    private readonly ILogger<ReceiveCaasFile> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IScreeningServiceData _screeningServiceData;

    public ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, ICallFunction callFunction,
        IScreeningServiceData screeningServiceData)
    {
        _logger = logger;
        _callFunction = callFunction;
        _screeningServiceData = screeningServiceData;
    }

    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        var downloadFilePath = string.Empty;
        try
        {
            _logger.LogInformation("Validating naming convention and file extension of {name}", name);
            if (!FileNameAndFileExtensionIsValid(name))
            {
                _logger.LogError(
                    "File name or file extension is invalid. Not in format BSS_ccyymmddhhmmss_n8.parquet. file Name: " +
                    name);
                await InsertValidationErrorIntoDatabase(name);
                return;
            }
            _logger.LogInformation("fetch number of records from file name {name}", name);
            var numberOfRecords = await GetNumberOfRecordsFromFileName(name);
            if (numberOfRecords == null) return;

            Cohort cohort = new()
            {
                FileName = name
            };
            var chunks = new List<Cohort>();
            var rowNumber = 0;

            try
            {

                try
                {
                    downloadFilePath = Path.Combine(Path.GetTempPath(), name);

                    _logger.LogInformation("Downloading the file {name} from the blob.", name);
                    using (var fileStream = File.Create(downloadFilePath))
                    {
                        blobStream.Seek(0, SeekOrigin.Begin);
                        blobStream.CopyTo(fileStream);
                    }


                    var screeningService = GetScreeningService(name);



                    _logger.LogInformation("screeningService {screeningService}", screeningService.ScreeningName);
                    _logger.LogInformation("Start reading the downloadedfile {name}.", name);
                    using (var rowReader = ParquetFile.CreateRowReader<ParticipantsParquetMap>(downloadFilePath))
                    {
                        for (var i = 0; i < rowReader.FileMetaData.NumRowGroups; ++i)
                        {
                            var values = rowReader.ReadRows(i);
                            foreach (var rec in values)
                            {
                                rowNumber++;

                                var participant = new Participant();
                                participant.RecordType = Convert.ToString(rec.RecordType);
                                participant.ChangeTimeStamp = Convert.ToString(rec.ChangeTimeStamp);
                                participant.SerialChangeNumber = Convert.ToString(rec.SerialChangeNumber);
                                participant.NhsNumber = Convert.ToString(rec.NhsNumber);
                                participant.SupersededByNhsNumber = Convert.ToString(rec.SupersededByNhsNumber);
                                participant.PrimaryCareProvider = Convert.ToString(rec.PrimaryCareProvider);
                                participant.PrimaryCareProviderEffectiveFromDate =
                                    Convert.ToString(rec.PrimaryCareEffectiveFromDate);
                                participant.CurrentPosting = Convert.ToString(rec.CurrentPosting);
                                participant.CurrentPostingEffectiveFromDate =
                                    Convert.ToString(rec.CurrentPostingEffectiveFromDate);
                                participant.NamePrefix = Convert.ToString(rec.NamePrefix);
                                participant.FirstName = Convert.ToString(rec.FirstName);
                                participant.OtherGivenNames = Convert.ToString(rec.OtherGivenNames);
                                participant.Surname = Convert.ToString(rec.SurnamePrefix);
                                participant.PreviousSurname = Convert.ToString(rec.PreviousSurnamePrefix);
                                participant.DateOfBirth = Convert.ToString(rec.DateOfBirth);
                                if (Enum.IsDefined(typeof(Gender), Convert.ToInt16(rec.Gender)))
                                {
                                    participant.Gender =
                                        (Gender)Enum.ToObject(typeof(Gender), Convert.ToInt16(rec.Gender));
                                }
                                participant.AddressLine1 = Convert.ToString(rec.AddressLine1);
                                participant.AddressLine2 = Convert.ToString(rec.AddressLine2);
                                participant.AddressLine3 = Convert.ToString(rec.AddressLine3);
                                participant.AddressLine4 = Convert.ToString(rec.AddressLine4);
                                participant.AddressLine5 = Convert.ToString(rec.AddressLine5);
                                participant.Postcode = Convert.ToString(rec.Postcode);
                                participant.PafKey = Convert.ToString(rec.PafKey);
                                participant.UsualAddressEffectiveFromDate =
                                    Convert.ToString(rec.UsualAddressEffectiveFromDate);
                                participant.ReasonForRemoval = Convert.ToString(rec.ReasonForRemoval);
                                participant.ReasonForRemovalEffectiveFromDate =
                                    Convert.ToString(rec.ReasonForRemovalEffectiveFromDate);
                                participant.DateOfDeath = Convert.ToString(rec.DateOfDeath);
                                if (Enum.IsDefined(typeof(Status), Convert.ToInt16(rec.DeathStatus)))
                                {
                                    participant.DeathStatus = (Status)Enum.ToObject(typeof(Status),
                                        Convert.ToInt16(rec.DeathStatus));
                                }
                                participant.TelephoneNumber = Convert.ToString(rec.TelephoneNumber);
                                participant.TelephoneNumberEffectiveFromDate =
                                    Convert.ToString(rec.TelephoneNumberEffectiveFromDate);
                                participant.MobileNumber = Convert.ToString(rec.MobileNumber);
                                participant.MobileNumberEffectiveFromDate =
                                    Convert.ToString(rec.MobileNumberEffectiveFromDate);
                                participant.EmailAddress = Convert.ToString(rec.EmailAddress);
                                participant.EmailAddressEffectiveFromDate =
                                    Convert.ToString(rec.EmailAddressEffectiveFromDate);
                                participant.IsInterpreterRequired = Convert.ToString(rec.IsInterpreterRequired);
                                participant.PreferredLanguage = Convert.ToString(rec.PreferredLanguage);
                                participant.InvalidFlag = Convert.ToString(rec.InvalidFlag);

                                cohort.Participants.Add(participant);

                                if (cohort.Participants.Count == 20000)
                                {
                                    chunks.Add(cohort);
                                    cohort.Participants.Clear();
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Reading completed for the file {name}. Total number of record is {rowNumber}.", name, rowNumber);
                    if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", rowNumber, ex.Message, ex.StackTrace);
                    await InsertValidationErrorIntoDatabase(name);
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
            catch (Exception ex)
            {
                _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
                return;
            }

            try
            {
                _logger.LogInformation("Start processing {rowNumber} rows of record from {name} file.", rowNumber, name);
                if (chunks.Count > 0)
                {
                    _logger.LogInformation("Start processing the files in chunks of 20000");
                    foreach (var chunk in chunks)
                    {
                        var json = JsonSerializer.Serialize(chunk);
                        await _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                        _logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                    }
                    _logger.LogInformation("Total {ChunksCount} number of chunks processed.", chunks.Count);
                }

                if (cohort.Participants.Count > 0)
                {
                    _logger.LogInformation("Start processing last remaining {CohortCount} Objects.", cohort.Participants.Count);
                    var json = JsonSerializer.Serialize(cohort);

                    await _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    _logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                }

                _logger.LogInformation("File {name} processed successfully. Total {rowNumber} Objects created.", name, rowNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError("Message:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
            return;
        }
        finally
        {
            if (File.Exists(downloadFilePath)) File.Delete(downloadFilePath);
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
            _logger.LogError("An error occurred while saving or moving the failed file {fileName}.", fileName);
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
        var match = Regex.Match(name, @"^\w{1,}_\d{14}_n([1-9]\d*|0)\.parquet$", RegexOptions.IgnoreCase);
        return match.Success;
    }

    private async Task<int?> GetNumberOfRecordsFromFileName(string name)
    {
        var str = name.Remove(name.IndexOf('.'));
        var numberOfRecords = str.Split('_')[2].Substring(1);

        if (int.TryParse(numberOfRecords, out var n))
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
