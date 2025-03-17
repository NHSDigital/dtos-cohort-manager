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
            throw new ArgumentException("Unable to create participant object", ex);
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
