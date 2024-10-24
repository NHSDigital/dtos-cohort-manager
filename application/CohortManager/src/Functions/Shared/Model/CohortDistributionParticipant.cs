namespace Model;

using Enums;
using System.Text.Json.Serialization;

public class CohortDistributionParticipant
{
    public string RecordType { get; set; }

    [JsonPropertyName("ParticipantId")]
    public string? ParticipantId { get; set; }

    [JsonPropertyName("NHS Number")]
    public string NhsNumber { get; set; }

    [JsonPropertyName("Superseded by NHS number")]
    public string? SupersededByNhsNumber { get; set; }

    [JsonPropertyName("Primary Care Provider")]
    public string? PrimaryCareProvider { get; set; }

    [JsonPropertyName("Primary Care Provider Business Effective From Date")]
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }

    [JsonPropertyName("Name Prefix")]
    public string? NamePrefix { get; set; }

    [JsonPropertyName("Given Name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("Other Given Name(s)")]
    public string? OtherGivenNames { get; set; }

    [JsonPropertyName("Family Name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("Previous Family Name")]
    public string? PreviousFamilyName { get; set; }

    [JsonPropertyName("Date of Birth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("Gender")]
    public Gender? Gender { get; set; }

    [JsonPropertyName("Address line 1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("Address line 2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("Address line 3")]
    public string? AddressLine3 { get; set; }

    [JsonPropertyName("Address line 4")]
    public string? AddressLine4 { get; set; }

    [JsonPropertyName("Address line 5")]
    public string? AddressLine5 { get; set; }

    [JsonPropertyName("Postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("Usual Address Business Effective From Date")]
    public string? UsualAddressEffectiveFromDate { get; set; }

    [JsonPropertyName("Date of Death")]
    public string? DateOfDeath { get; set; }

    [JsonPropertyName("Telephone Number (Home)")]
    public string? TelephoneNumber { get; set; }

    [JsonPropertyName("Telephone Number (Home) Business Effective From Date")]
    public string? TelephoneNumberEffectiveFromDate { get; set; }

    [JsonPropertyName("Telephone Number (Mobile)")]
    public string? MobileNumber { get; set; }

    [JsonPropertyName("Telephone Number (Mobile) Business Effective From Date")]
    public string? MobileNumberEffectiveFromDate { get; set; }

    [JsonPropertyName("E-mail address (Home)")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("E-mail address (Home) Business Effective From Date")]
    public string? EmailAddressEffectiveFromDate { get; set; }

    [JsonPropertyName("Preferred Language")]
    public string? PreferredLanguage { get; set; }
    public string? IsInterpreterRequired { get; set; }

    [JsonPropertyName("Reason for Removal")]
    public string? ReasonForRemoval { get; set; }

    [JsonPropertyName("Reason for Removal Business Effective From Date")]
    public string? ReasonForRemovalEffectiveFromDate { get; set; }

    [JsonPropertyName("RecordInsertDateTime")]
    public string? RecordInsertDateTime { get; set; }

    [JsonPropertyName("RecordUpdateDateTime")]
    public string? RecordUpdateDateTime { get; set; }

    [JsonPropertyName("Extracted")]
    public string? Extracted { get; set; }
    public int? ServiceProviderId { get; set; }
    public string? ScreeningAcronym { get; set; }
    public string? ScreeningServiceId { get; set; }
    public string? ScreeningName { get; set; }
    public string? RequestId { get; set; }
    public string? EligibilityFlag { get; set; }

    [JsonPropertyName("Current Posting")]
    public string? CurrentPosting { get; set; }

    [JsonPropertyName("Current Posting Business Effective From Date")]
    public string? CurrentPostingEffectiveFromDate { get; set; }
}
