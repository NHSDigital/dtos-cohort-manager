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
using NHS.Screening.ReceiveCaasFile;

public class ReceiveCaasFileHelper : IReceiveCaasFileHelper
{
    private readonly ILogger<ReceiveCaasFileHelper> _logger;
    private readonly ICallFunction _callFunction;
    public ReceiveCaasFileHelper(ILogger<ReceiveCaasFileHelper> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    public async Task<Participant?> MapParticipant(ParticipantsParquetMap rec, string screeningId, string ScreeningName, string name)
    {

        try
        {
            return new Participant()
            {
                ScreeningId = screeningId,
                ScreeningName = ScreeningName,
                RecordType = Convert.ToString(rec.RecordType),
                ChangeTimeStamp = Convert.ToString(rec.ChangeTimeStamp),
                SerialChangeNumber = Convert.ToString(rec.SerialChangeNumber),
                NhsNumber = Convert.ToString(rec.NhsNumber),
                SupersededByNhsNumber = Convert.ToString(rec.SupersededByNhsNumber),
                PrimaryCareProvider = Convert.ToString(rec.PrimaryCareProvider),
                PrimaryCareProviderEffectiveFromDate = Convert.ToString(rec.PrimaryCareEffectiveFromDate),
                CurrentPosting = Convert.ToString(rec.CurrentPosting),
                CurrentPostingEffectiveFromDate = Convert.ToString(rec.CurrentPostingEffectiveFromDate),
                NamePrefix = Convert.ToString(rec.NamePrefix),
                FirstName = Convert.ToString(rec.FirstName),
                OtherGivenNames = Convert.ToString(rec.OtherGivenNames),
                FamilyName = Convert.ToString(rec.SurnamePrefix),
                PreviousFamilyName = Convert.ToString(rec.PreviousSurnamePrefix),
                DateOfBirth = Convert.ToString(rec.DateOfBirth),
                Gender = (Gender)rec.Gender.GetValueOrDefault(),
                AddressLine1 = Convert.ToString(rec.AddressLine1),
                AddressLine2 = Convert.ToString(rec.AddressLine2),
                AddressLine3 = Convert.ToString(rec.AddressLine3),
                AddressLine4 = Convert.ToString(rec.AddressLine4),
                AddressLine5 = Convert.ToString(rec.AddressLine5),
                Postcode = Convert.ToString(rec.Postcode),
                PafKey = Convert.ToString(rec.PafKey),
                UsualAddressEffectiveFromDate = rec.UsualAddressEffectiveFromDate,
                ReasonForRemoval = Convert.ToString(rec.ReasonForRemoval),
                ReasonForRemovalEffectiveFromDate = rec.ReasonForRemovalEffectiveFromDate,
                DateOfDeath = Convert.ToString(rec.DateOfDeath),
                DeathStatus = rec.DeathStatus.HasValue ? (Status)rec.DeathStatus.GetValueOrDefault() : null,
                TelephoneNumber = Convert.ToString(rec.TelephoneNumber),
                TelephoneNumberEffectiveFromDate = Convert.ToString(rec.TelephoneNumberEffectiveFromDate),
                MobileNumber = Convert.ToString(rec.MobileNumber),
                MobileNumberEffectiveFromDate = Convert.ToString(rec.MobileNumberEffectiveFromDate),
                EmailAddress = Convert.ToString(rec.EmailAddress),
                EmailAddressEffectiveFromDate = rec.EmailAddressEffectiveFromDate,
                IsInterpreterRequired = Convert.ToString(rec.IsInterpreterRequired.GetValueOrDefault(false) ? "1" : "0"),
                PreferredLanguage = Convert.ToString(rec.PreferredLanguage),
                InvalidFlag = Convert.ToString(rec.InvalidFlag),
                EligibilityFlag = BitStringFromNullableBoolean(rec.EligibilityFlag),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create object .\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            await InsertValidationErrorIntoDatabase(name, JsonSerializer.Serialize(new Participant()));
            return null;
        }
    }

    private static string? BitStringFromNullableBoolean(bool? boolValue)
    {
        if (!boolValue.HasValue)
        {
            return null;
        }
        return boolValue.Value ? "1" : "0";
    }

    public async Task InsertValidationErrorIntoDatabase(string fileName, string errorRecord)
    {
        var fileValidationURL = GetUrlFromEnvironment("FileValidationURL");
        var json = JsonSerializer.Serialize(new ValidationException()
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


    public async Task<bool> CheckFileName(string name, FileNameParser fileNameParser, string errorMessage)
    {
        _logger.LogInformation("loading file from blob {name}", name);

        // make sure that that file name is valid
        if (!fileNameParser.IsValid)
        {
            await InsertValidationErrorIntoDatabase(name, errorMessage);
            return false;
        }
        return true;
    }
}
