namespace Model;

using System.Text.Json.Serialization;
using Model.Enums;

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
            DateOfBirth = DateOfBirth,
            Gender = (short?)(Gender.HasValue ? (Gender?)Gender.Value : null),
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            PostCode = Postcode,
            PafKey = PafKey,
            UsualAddressFromDate = UsualAddressEffectiveFromDate,
            DateOfDeath = DateOfDeath,
            DeathStatus = (short?)(DeathStatus.HasValue ? (Status?)DeathStatus.Value : null),
            TelephoneNumberHome = TelephoneNumber,
            TelephoneNumberHomeFromDate = TelephoneNumberEffectiveFromDate,
            TelephoneNumberMob = MobileNumber,
            TelephoneNumberMobFromDate = MobileNumberEffectiveFromDate,
            EmailAddressHome = EmailAddress,
            EmailAddressHomeFromDate = EmailAddressEffectiveFromDate,
            PreferredLanguage = PreferredLanguage,
            InterpreterRequired = IsInterpreterRequired?.ToLower() switch
                                    {
                                        "true" => 1,
                                        "false" => 0,
                                        _ => 0 // Default fallback (or throw an error)
                                    }
        };
    }

}
