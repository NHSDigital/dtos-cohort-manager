
namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Storage.Blobs;
using ChoETL;
using Model;
using Common;
using System.Globalization;
using System.Net;
using System.IO;
using Model.Enums;
using System.Collections.Generic;

public class ReceiveCaasFile(ILogger<ReceiveCaasFile> logger, ICallFunction callFunction)
{
    [Function(nameof(ReceiveCaasFile))]
    public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream strm, string name)
    {
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
                var blobs = container.GetBlobs().Where(x => x.Name.Equals(name));

                try
                {
                    foreach (var item in blobs)
                    {
                        var blob = container.GetBlobClient(item.Name);
                        await using var stream = await blob.OpenReadAsync();

                       foreach (dynamic e in new ChoParquetReader(stream))
                       {
                           rowNumber++;

                           Participant participant = new Participant();
                           participant.RecordType = Convert.ToString(e.Record_Type);
                           participant.ChangeTimeStamp = e.Change_Time_Stamp.ToString("F0", CultureInfo.InvariantCulture);
                           participant.SerialChangeNumber = Convert.ToString(e.Serial_Change_Number);
                           participant.NhsNumber = Convert.ToString(e.NHS_Number);
                           participant.SupersededByNhsNumber = Convert.ToString(e.Superseded_by_NHS_number);
                           participant.PrimaryCareProvider =Convert.ToString( e.Primary_Care_Provider);
                           participant.PrimaryCareProviderEffectiveFromDate = Convert.ToString(e.Primary_Care_Provider_Business_Effective_From_Date);
                           participant.CurrentPosting = Convert.ToString(e.Current_Posting);
                           participant.CurrentPostingEffectiveFromDate = Convert.ToString(e.Current_Posting_Business_Effective_From_Date);
                           participant.PreviousPosting = Convert.ToString(e.Previous_Posting);
                           participant.PreviousPostingEffectiveFromDate = Convert.ToString(e.Previous_Posting_Business_Effective_To_Date);
                           participant.NamePrefix = Convert.ToString(e.Name_Prefix);
                           participant.FirstName = Convert.ToString(e.Given_Name);
                           participant.OtherGivenNames = Convert.ToString(e.Other_Given_Name);
                           participant.Surname = Convert.ToString(e.Family_Name);
                           participant.PreviousSurname = Convert.ToString(e.Previous_Family_Name);
                           participant.DateOfBirth = Convert.ToString(e.Date_of_Birth);
                           if (Enum.IsDefined(typeof(Gender),Convert.ToInt16(e.Gender)))
                            {
                                participant.Gender = (Gender)Enum.ToObject(typeof(Gender), e.Gender);
                            }
                            else
                            {
                                logger.LogError($"Validation failed for field name 'Gender' on line {rowNumber}. File name: {name}.");
                                await InsertValidationErrorIntoDatabase(name);
                                return;
                            }
                           participant.AddressLine1 = Convert.ToString(e.Address_line_1);
                           participant.AddressLine2 = Convert.ToString(e.Address_line_2);
                           participant.AddressLine3 = Convert.ToString(e.Address_line_3);
                           participant.AddressLine4 = Convert.ToString(e.Address_line_4);
                           participant.AddressLine5 = Convert.ToString(e.Address_line_5);
                           participant.Postcode = Convert.ToString(e.Postcode);
                           participant.PafKey = Convert.ToString(e.PAF_key);
                           participant.UsualAddressEffectiveFromDate = Convert.ToString(e.Usual_Address_Business_Effective_From_Date);
                           participant.ReasonForRemoval = Convert.ToString(e.Reason_for_Removal);
                           participant.ReasonForRemovalEffectiveFromDate = Convert.ToString(e.Reason_for_Removal_Business_Effective_From_Date);
                           participant.DateOfDeath = Convert.ToString(e.Date_of_Death);
                           if (Enum.IsDefined(typeof(Status),Convert.ToInt16(e.Death_Status)))
                            {
                                participant.DeathStatus = (Status)Enum.ToObject(typeof(Status), e.Death_Status);
                            }
                            else
                            {
                                logger.LogError($"Validation failed for field name 'Status' on line {rowNumber}. File name: {name}.");
                                await InsertValidationErrorIntoDatabase(name);
                                return;
                            }
                           participant.TelephoneNumber = Convert.ToString(e.Telephone_Number_Home);
                           participant.TelephoneNumberEffectiveFromDate = Convert.ToString(e.Telephone_Number_Home_Business_Effective_From_Date);
                           participant.MobileNumber = Convert.ToString(e.Telephone_Number_Mobile);
                           participant.MobileNumberEffectiveFromDate = Convert.ToString(e.Telephone_Number_Mobile_Business_Effective_From_Date);
                           participant.EmailAddress = Convert.ToString(e.Email_address_Home);
                           participant.EmailAddressEffectiveFromDate = Convert.ToString(e.Email_address_Home_Business_Effective_From_Date);
                           participant.PreferredLanguage = Convert.ToString(e.Preferred_Language);
                           participant.IsInterpreterRequired = Convert.ToString(e.Interpreter_required);
                           participant.InvalidFlag = Convert.ToString(e.Invalid_Flag);
                           participant.RecordIdentifier = Convert.ToString(e.Record_Identifier);
                           participant.ChangeReasonCode = Convert.ToString(e.Change_Reason_Code);

                           cohort.Participants.Add(participant);

                           if (cohort.Participants.Count == 1000)
                           {
                             //124928
                               chunks.Add(cohort);
                               cohort.Participants.Clear();
                           }
                       }

                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", rowNumber, ex.Message, ex.StackTrace);
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
                logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
                return;
            }
            try
            {
                if(chunks.Count > 0)
                {
                    foreach (Cohort chunk in chunks)
                    {
                    var json = JsonSerializer.Serialize(chunk);
                    await callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                    }
                }
                 if(cohort.Participants.Count > 0)
                {
                    var json = JsonSerializer.Serialize(cohort);
                    await callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    logger.LogInformation("Created {CohortCount} Objects.", cohort.Participants.Count);
                }

            }
            catch (Exception ex)
            {
                logger.LogError("Unable to call function.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
                await InsertValidationErrorIntoDatabase(name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("{MessageType} validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
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
