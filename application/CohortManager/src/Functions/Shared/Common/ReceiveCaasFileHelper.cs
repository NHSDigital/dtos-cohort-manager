namespace Common;

using Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Model;
using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Model.Enums;
using System.Threading.Tasks;

public class ReceiveCaasFileHelper : IReceiveCaasFileHelper
{
    private readonly ILogger<ReceiveCaasFileHelper> _logger;
    private readonly ICallFunction _callFunction;
    public ReceiveCaasFileHelper(ILogger<ReceiveCaasFileHelper> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    public async Task<bool> InitialChecks(Stream? blobStream, string name)
    {
        _logger.LogInformation("Blob & file extension check: {Name}", name);
        if (blobStream == null)
        {
            _logger.LogError("Blob is empty.");
            await InsertValidationErrorIntoDatabase(name, "N/A");
            return false;
        }
        else if (!FileNameAndFileExtensionIsValid(name))
        {
            _logger.LogError(
                "File name or file extension is invalid. Not in format BSS_ccyymmddhhmmss_n8.parquet. file Name: {Name}",
                name);
            await InsertValidationErrorIntoDatabase(name, "N/A");
            return false;
        }
        else
        {
            return true;
        }
    }

    private static bool FileNameAndFileExtensionIsValid(string name)
    {
        /* for file format BSS_ccyymmddhhmmss_n8.csv
        '^\w{1,}_' Matches the screening acronym, it could be anything before the first underscore
        '\d{14}' Matches exactly 14 digits, representing ccyymmddhhmmss
        '_n' Matches the literal _n
        '([1-9]\d*|0)' Matches any number with no leading zeros OR The number 0.
        '\.csv$' matches .csv at the end of the string */
        var match = Regex.Match(name, @"^\w{1,}_\d{14}_n([1-9]\d*|0)\.parquet$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
        return match.Success;
    }

    public async Task<Participant?> MapParticipant(ParticipantsParquetMap rec, Participant participant, string name, int rowNumber)
    {
        try
        {
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
            participant.FamilyName = Convert.ToString(rec.SurnamePrefix);
            participant.PreviousFamilyName = Convert.ToString(rec.PreviousSurnamePrefix);
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
            participant.IsInterpreterRequired = Convert.ToString(rec.IsInterpreterRequired.GetValueOrDefault(true) ? "1" : "0");
            participant.PreferredLanguage = Convert.ToString(rec.PreferredLanguage);
            participant.InvalidFlag = Convert.ToString(rec.InvalidFlag.GetValueOrDefault(true) ? "1" : "0");
            participant.EligibilityFlag = Convert.ToString(rec.EligibilityFlag.GetValueOrDefault(true) ? "1" : "0");

            return participant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", rowNumber, ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name, JsonSerializer.Serialize(participant));
            return null;
        }
    }

    public async Task SerializeParquetFile(List<Cohort> chunks, Cohort cohort, string filename, int rowNumber)
    {
        try
        {
            var targetFunctionUrl = GetUrlFromEnvironment("targetFunction");
            if (chunks.Count > 0)
            {
                _logger.LogInformation("Start processing the files in chunks");
                foreach (var chunk in chunks)
                {
                    var json = JsonSerializer.Serialize(chunk);
                    await _callFunction.SendPost(targetFunctionUrl, json);
                }
            }

            if (cohort.Participants.Count > 0)
            {
                _logger.LogInformation("Start processing last remaining {CohortCount} Objects.", cohort.Participants.Count);
                var json = JsonSerializer.Serialize(cohort);
                await _callFunction.SendPost(targetFunctionUrl, json);
            }
            _logger.LogInformation("Created {CohortCount} Objects.", rowNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stack Trace: {ExStackTrace}\nMessage:{ExMessage}", ex.StackTrace, ex.Message);
            await InsertValidationErrorIntoDatabase(filename, "N/A");
        }
    }

    public async Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord)
    {
        var fileValidationURL = GetUrlFromEnvironment("FileValidationURL");
        var json = JsonSerializer.Serialize<Model.ValidationException>(new Model.ValidationException()
        {
            FileName = fileName,
            ErrorRecord = errorRecord
        });

        var result = await _callFunction.SendPost(fileValidationURL, json);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("An error occurred while saving or moving the failed file: {FileName}.", fileName);
        }
        _logger.LogInformation("File failed checks and has been moved to the poison blob storage");
    }

    public string GetUrlFromEnvironment(string key)
    {
        var url = Environment.GetEnvironmentVariable(key);
        if (url == null)
        {
            _logger.LogError("Environment variable is not set.");
            throw new InvalidOperationException("Environment variable is not set.");
        }
        return url;
    }
}
