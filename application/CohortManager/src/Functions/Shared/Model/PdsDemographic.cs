namespace Model;

using System.Data;
using System.Text.Json.Serialization;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

public class PdsDemographic : Demographic
{
    [JsonPropertyOrder(900)]
    public string? ReasonForRemoval { get; set; }
    [JsonPropertyOrder(901)]
    public string? RemovalEffectiveFromDate { get; set; }
    [JsonPropertyOrder(902)]
    public string? RemovalEffectiveToDate { get; set; }
    [JsonPropertyOrder(903)]
    public string? ConfidentialityCode { get; set; } = "";
    public PdsDemographic() { }

    public ParticipantDemographic ToParticipantDemographic()
    {
        return new ParticipantDemographic
        {
            NhsNumber = long.Parse(NhsNumber),
            PrimaryCareProvider = PrimaryCareProvider,
            PrimaryCareProviderFromDate = PrimaryCareProviderEffectiveFromDate,
            NamePrefix = NamePrefix,
            GivenName = FirstName,
            OtherGivenName = OtherGivenNames,
            FamilyName = FamilyName,
            PreviousFamilyName = PreviousFamilyName,
            DateOfBirth = MappingUtilities.ParseDates(DateOfBirth!)?.ToString("yyyyMMdd"),
            Gender = (short?)(Gender.HasValue ? (Gender?)Gender.Value : null),
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            PostCode = Postcode,
            PafKey = PafKey,
            UsualAddressFromDate = MappingUtilities.ParseDates(UsualAddressEffectiveFromDate!)?.ToString("yyyyMMdd"),
            DateOfDeath = DateOfDeath,
            DeathStatus = (short?)(DeathStatus.HasValue ? (Status?)DeathStatus.Value : null),
            TelephoneNumberHome = TelephoneNumber,
            TelephoneNumberHomeFromDate = MappingUtilities.ParseDates(TelephoneNumberEffectiveFromDate!)?.ToString("yyyy-MM-dd"),
            TelephoneNumberMob = MobileNumber,
            TelephoneNumberMobFromDate = MappingUtilities.ParseDates(MobileNumberEffectiveFromDate!)?.ToString("yyyyMMdd"),
            EmailAddressHome = EmailAddress,
            EmailAddressHomeFromDate = MappingUtilities.ParseDates(EmailAddressEffectiveFromDate!)?.ToString("yyyyMMdd"),
            PreferredLanguage = PreferredLanguage,
            RecordInsertDateTime = DateTime.UtcNow,
            InterpreterRequired = IsInterpreterRequired?.ToLower() switch
            {
                "true" => 1,
                "false" => 0,
                null => throw new ArgumentNullException(nameof(IsInterpreterRequired)),
                _ => throw new ArgumentException($"Invalid IsInterpreterRequired value '{IsInterpreterRequired}'. Must be 'true' or 'false'")
            }
        };
    }

    public CohortDistributionParticipant ToCohortDistributionParticipant()
    {
        return new CohortDistributionParticipant
        {
            NhsNumber = !string.IsNullOrEmpty(NhsNumber) ? NhsNumber : throw new FormatException("NHS number cannot be null or empty."),
            SupersededByNhsNumber = !string.IsNullOrEmpty(SupersededByNhsNumber) ? SupersededByNhsNumber : null,
            PrimaryCareProvider = PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = PrimaryCareProviderEffectiveFromDate,
            CurrentPosting = CurrentPosting,
            CurrentPostingEffectiveFromDate = CurrentPostingEffectiveFromDate,
            NamePrefix = NamePrefix,
            FirstName = FirstName,
            OtherGivenNames = OtherGivenNames,
            FamilyName = FamilyName,
            PreviousFamilyName = PreviousFamilyName,
            DateOfBirth = DateOfBirth,
            Gender = Gender.GetValueOrDefault(),
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            Postcode = Postcode,
            UsualAddressEffectiveFromDate = UsualAddressEffectiveFromDate,
            DateOfDeath = DateOfDeath,
            TelephoneNumber = TelephoneNumber,
            TelephoneNumberEffectiveFromDate = TelephoneNumberEffectiveFromDate,
            MobileNumber = MobileNumber,
            MobileNumberEffectiveFromDate = MobileNumberEffectiveFromDate,
            EmailAddress = EmailAddress,
            EmailAddressEffectiveFromDate = EmailAddressEffectiveFromDate,
            PreferredLanguage = PreferredLanguage,
            IsInterpreterRequired = IsInterpreterRequired,
            InvalidFlag = InvalidFlag,
            RecordInsertDateTime = RecordInsertDateTime,
            RecordUpdateDateTime = RecordUpdateDateTime,
            ReasonForRemoval = ReasonForRemoval,
            ReasonForRemovalEffectiveFromDate = RemovalEffectiveFromDate
        };
    }

}
