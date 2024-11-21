namespace Model;

using Enums;
using System.Text.Json.Serialization;

public class CohortDistributionParticipant
{
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }
    [JsonPropertyName("nhs_number")]
    public string NhsNumber { get; set; }
    [JsonPropertyName("superseded_by_nhs_number")]
    public string? SupersededByNhsNumber { get; set; }
    [JsonPropertyName("primary_care_provider")]
    public string? PrimaryCareProvider { get; set; }
    [JsonPropertyName("primary_care_provider_eff_from_date")]
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
    [JsonPropertyName("name_prefix")]
    public string? NamePrefix { get; set; }
    [JsonPropertyName("given_name")]
    public string? FirstName { get; set; }
    [JsonPropertyName("other_given_names")]
    public string? OtherGivenNames { get; set; }
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }
    [JsonPropertyName("previous_family_name")]
    public string? PreviousFamilyName { get; set; }
    [JsonPropertyName("birth_date")]
    public string? DateOfBirth { get; set; }
    [JsonPropertyName("gender_code")]
    public Gender? Gender { get; set; }
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }
    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }
    [JsonPropertyName("address_line_3")]
    public string? AddressLine3 { get; set; }
    [JsonPropertyName("address_line_4")]
    public string? AddressLine4 { get; set; }
    [JsonPropertyName("address_line_5")]
    public string? AddressLine5 { get; set; }
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
    [JsonPropertyName("usual_address_eff_from_date")]
    public string? UsualAddressEffectiveFromDate { get; set; }
    [JsonPropertyName("death_date")]
    public string? DateOfDeath { get; set; }
    [JsonPropertyName("telephone_number_home")]
    public string? TelephoneNumber { get; set; }
    [JsonPropertyName("telephone_number_home_eff_from_date")]
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    [JsonPropertyName("telephone_number_mobile")]
    public string? MobileNumber { get; set; }
    [JsonPropertyName("telephone_number_mobile_eff_from_date")]
    public string? MobileNumberEffectiveFromDate { get; set; }
    [JsonPropertyName("email_address_home")]
    public string? EmailAddress { get; set; }
    [JsonPropertyName("email_address_home_eff_from_date")]
    public string? EmailAddressEffectiveFromDate { get; set; }
    [JsonPropertyName("preferred_language")]
    public string? PreferredLanguage { get; set; }
    [JsonPropertyName("interpreter_required")]
    public string? IsInterpreterRequired { get; set; }
    [JsonPropertyName("reason_for_removal")]
    public string? ReasonForRemoval { get; set; }
    [JsonPropertyName("reason_removal_eff_from_date")]
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    [JsonIgnore]
    public string? RecordInsertDateTime { get; set; }
    [JsonIgnore]
    public string? RecordUpdateDateTime { get; set; }
    [JsonIgnore]
    public string? Extracted { get; set; }
    [JsonIgnore]
    public int? ServiceProviderId { get; set; }
    [JsonIgnore]
    public string? ScreeningAcronym { get; set; }
    [JsonIgnore]
    public string? ScreeningServiceId { get; set; }
    [JsonIgnore]
    public string? ScreeningName { get; set; }
    [JsonIgnore]
    public string? EligibilityFlag { get; set; }
    [JsonIgnore]
    public string? CurrentPosting { get; set; }
    [JsonIgnore]
    public string? CurrentPostingEffectiveFromDate { get; set; }
    [JsonIgnore]
    public string? ParticipantId { get; set; }
    public string RecordType { get; set; }
}
