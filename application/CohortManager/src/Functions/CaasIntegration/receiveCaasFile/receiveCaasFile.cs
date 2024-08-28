
namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Storage.Blobs;
using Model;
using Common;
using System.Net;
using System.IO;
using Model.Enums;
using System.Collections.Generic;
using ParquetSharp.RowOriented;
using System;

public class ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, ICallFunction callFunction)
{
    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream strm, string name)
    {
        string downloadFilePath = string.Empty;
        try
        {
            FileExtensionCheck(name);

            Cohort cohort = new()
            {
                FileName = name
            };
            List<Cohort> chunks = new List<Cohort>();
            var rowNumber = 0;

            try
            {
                var azureStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var blobContainerName = Environment.GetEnvironmentVariable("BlobContainerName");

                BlobServiceClient blobServiceClient = new BlobServiceClient(azureStorage);
                BlobContainerClient container = blobServiceClient.GetBlobContainerClient(blobContainerName);

                try
                {
                    downloadFilePath = Path.Combine(Path.GetTempPath(), name + ".parquet");
                    var blob = container.GetBlobClient(name);
                    await blob.DownloadToAsync(downloadFilePath);

                    using (var rowReader = ParquetFile.CreateRowReader<ParticipantsParquetMap>(downloadFilePath))
                    {
                        for (int i = 0; i < rowReader.FileMetaData.NumRowGroups; ++i)
                        {
                            var values = rowReader.ReadRows(i);
                            foreach (ParticipantsParquetMap rec in values)
                            {
                                rowNumber++;

                                Participant participant = new Participant();
                                participant.RecordType = Convert.ToString(rec.RecordType);
                                participant.ChangeTimeStamp = Convert.ToString(rec.ChangeTimeStamp);
                                participant.SerialChangeNumber = Convert.ToString(rec.SerialChangeNumber);
                                participant.NhsNumber = Convert.ToString(rec.NhsNumber);
                                participant.SupersededByNhsNumber = Convert.ToString(rec.SupersededByNhsNumber);
                                participant.PrimaryCareProvider = Convert.ToString(rec.PrimaryCareProvider);
                                participant.PrimaryCareProviderEffectiveFromDate =
                                    Convert.ToString(rec.PrimaryCareProviderEffectiveFromDate);
                                participant.CurrentPosting = Convert.ToString(rec.CurrentPosting);
                                participant.CurrentPostingEffectiveFromDate =
                                    Convert.ToString(rec.CurrentPostingEffectiveFromDate);
                                participant.PreviousPosting = Convert.ToString(rec.PreviousPosting);
                                participant.PreviousPostingEffectiveFromDate =
                                    Convert.ToString(rec.PreviousPostingEffectiveFromDate);
                                participant.NamePrefix = Convert.ToString(rec.NamePrefix);
                                participant.FirstName = Convert.ToString(rec.FirstName);
                                participant.OtherGivenNames = Convert.ToString(rec.OtherGivenNames);
                                participant.Surname = Convert.ToString(rec.SurnamePrefix);
                                participant.PreviousSurname = Convert.ToString(rec.PreviousSurnamePrefix);
                                participant.DateOfBirth = Convert.ToString(rec.DateOfBirth);
                                if (Enum.IsDefined(typeof(Gender), Convert.ToInt16(rec.Gender)))
                                {
                                    participant.Gender = (Gender)Enum.ToObject(typeof(Gender), Convert.ToInt16(rec.Gender));
                                }
                                else
                                {
                                    logger.LogError($"Validation failed for field name 'Gender' on line {rowNumber}. File name: {name}.");
                                    await InsertValidationErrorIntoDatabase(name);
                                    return;
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
                                    participant.DeathStatus = (Status)Enum.ToObject(typeof(Status), Convert.ToInt16(rec.DeathStatus));
                                }
                                else
                                {
                                    logger.LogError($"Validation failed for field name 'Status' on line {rowNumber}. File name: {name}.");
                                    await InsertValidationErrorIntoDatabase(name);
                                    return;
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
                                participant.RecordIdentifier = Convert.ToString(rec.RecordIdentifier);
                                participant.ChangeReasonCode = Convert.ToString(rec.ChangeReasonCode);

                                cohort.Participants.Add(participant);

                                if (cohort.Participants.Count == 20000)
                                {
                                    chunks.Add(cohort);
                                    cohort.Participants.Clear();
                                }
                            }
                        }
                    }

                    if (File.Exists(downloadFilePath))
                    {
                        File.Delete(downloadFilePath);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        "Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                        rowNumber, ex.Message, ex.StackTrace);
                    await InsertValidationErrorIntoDatabase(name);
                }

                if (rowNumber == 0)
                {
                    logger.LogError("File contains no records. File name:" + name);
                    await InsertValidationErrorIntoDatabase(name);
                    return;
                }

            }
            catch (Exception ex)
            {
                logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
                return;
            }

            try
            {
                if (chunks.Count > 0)
                {
                    foreach (Cohort chunk in chunks)
                    {
                        var json = JsonSerializer.Serialize(chunk);
                        await callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                        logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                    }
                }

                if (cohort.Participants.Count > 0)
                {
                    var json = JsonSerializer.Serialize(cohort);
                    await callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                }

            }
            catch (Exception ex)
            {
                logger.LogError("Unable to call function.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                    ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}",
                ex.GetType().Name, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name);
            return;
        }
        finally
        {
            if (File.Exists(downloadFilePath))
            {
                File.Delete(downloadFilePath);
            }
        }
    }

    private async Task InsertValidationErrorIntoDatabase(string fileName)
    {
        var json = JsonSerializer.Serialize<Model.ValidationException>(new Model.ValidationException()
        {
            RuleId = 1,
            FileName = fileName
        });

        var result = await callFunction.SendPost(Environment.GetEnvironmentVariable("FileValidationURL"), json);
        if (result.StatusCode == HttpStatusCode.OK)
        {
            logger.LogInformation("file failed checks and has been moved to the poison blob storage");
        }
        logger.LogError("there was a problem saving and or moving the failed file");
    }

    private static void FileExtensionCheck(string name)
    {
        var fileExtension = Path.GetExtension(name).ToLower();
        if (fileExtension != FileFormats.Parquet)
        {
            throw new NotSupportedException("Invalid file type. Only parquet files are allowed.");
        };
    }
}
