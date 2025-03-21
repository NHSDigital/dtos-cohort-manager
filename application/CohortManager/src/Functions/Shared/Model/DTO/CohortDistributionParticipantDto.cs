namespace Model.DTO;

using Enums;
using System.Text.Json.Serialization;

public class CohortDistributionParticipantDto
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
    public int IsInterpreterRequired { get; set; }
    [JsonPropertyName("reason_for_removal")]
    public string? ReasonForRemoval { get; set; }
    [JsonPropertyName("reason_removal_eff_from_date")]
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    [JsonIgnore]
    public string? ParticipantId { get; set; }
    [JsonIgnore]
    public string? IsExtracted { get; set; }

    public CohortDistributionParticipantDto() {
        
    }
    public CohortDistributionParticipantDto(CohortDistribution cohortDistribution) {
        NhsNumber = cohortDistribution.NHSNumber.ToString();
        SupersededByNhsNumber = cohortDistribution.SupersededNHSNumber.ToString();
        PrimaryCareProvider = cohortDistribution.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = cohortDistribution.PrimaryCareProviderDate.ToString();
        NamePrefix = cohortDistribution.NamePrefix;
        FirstName = cohortDistribution.GivenName;
        OtherGivenNames = cohortDistribution.OtherGivenName;
        FamilyName = cohortDistribution.FamilyName;
        PreviousFamilyName = cohortDistribution.PreviousFamilyName;
        DateOfBirth = cohortDistribution.DateOfBirth.ToString();
        Gender = genderConverter(cohortDistribution.Gender);
        AddressLine1 = cohortDistribution.AddressLine1;
        AddressLine2 = cohortDistribution.AddressLine2;
        AddressLine3 = cohortDistribution.AddressLine3;
        AddressLine4 = cohortDistribution.AddressLine4;
        AddressLine5 = cohortDistribution.AddressLine5;
        Postcode = cohortDistribution.PostCode;
        UsualAddressEffectiveFromDate = cohortDistribution.UsualAddressFromDt.ToString();
        DateOfDeath = cohortDistribution.DateOfDeath.ToString();
        TelephoneNumber = cohortDistribution.TelephoneNumberHome;
        TelephoneNumberEffectiveFromDate = cohortDistribution.TelephoneNumberHomeFromDt.ToString();
        MobileNumber = cohortDistribution.TelephoneNumberMob;
        MobileNumberEffectiveFromDate = cohortDistribution.TelephoneNumberMobFromDt.ToString();
        EmailAddress = cohortDistribution.EmailAddressHome;
        EmailAddressEffectiveFromDate = cohortDistribution.EmailAddressHomeFromDt.ToString();
        PreferredLanguage = cohortDistribution.PreferredLanguage;
        IsInterpreterRequired = cohortDistribution.InterpreterRequired;
        ReasonForRemoval = cohortDistribution.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = cohortDistribution.ReasonForRemovalDate.ToString();
        ParticipantId = cohortDistribution.ParticipantId.ToString();
        IsExtracted = cohortDistribution.IsExtracted.ToString();
    }

    private static Gender genderConverter(short gender) 
    {
        if (gender == 1 ) 
        {
            return Enums.Gender.Male;
        } 
        else if (gender == 2) 
        {
            return Enums.Gender.Female;
        }
        else if (gender == 9)
        {
            return Enums.Gender.NotSpecified;
        } 
        else
        {
            return Enums.Gender.NotKnown;
        }
    }

}