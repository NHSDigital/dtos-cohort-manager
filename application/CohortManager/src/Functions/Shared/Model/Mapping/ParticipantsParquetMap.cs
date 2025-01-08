namespace Model;
using ParquetSharp.RowOriented;

public struct ParticipantsParquetMap
{
    [MapToColumn("record_type")]
    public string? RecordType { get; set; }

    [MapToColumn("change_time_stamp")]
    public Int64? ChangeTimeStamp { get; set; }

    [MapToColumn("serial_change_number")]
    public Int64? SerialChangeNumber { get; set; }

    [MapToColumn("nhs_number")]
    public Int64? NhsNumber { get; set; }

    [MapToColumn("superseded_by_nhs_number")]
    public Int64? SupersededByNhsNumber { get; set; }

    [MapToColumn("primary_care_provider")]
    public string? PrimaryCareProvider { get; set; }

    [MapToColumn("primary_care_effective_from_date")]
    public string? PrimaryCareEffectiveFromDate { get; set; }

    [MapToColumn("current_posting")]
    public string? CurrentPosting { get; set; }

    [MapToColumn("current_posting_effective_from_date")]
    public string? CurrentPostingEffectiveFromDate { get; set; }

    [MapToColumn("name_prefix")]
    public string? NamePrefix { get; set; }

    [MapToColumn("given_name")]
    public string? FirstName { get; set; }

    [MapToColumn("other_given_name")]
    public string? OtherGivenNames { get; set; }

    [MapToColumn("family_name")]
    public string? SurnamePrefix { get; set; }

    [MapToColumn("previous_family_name")]
    public string? PreviousSurnamePrefix { get; set; }

    [MapToColumn("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [MapToColumn("gender")]
    public Int64? Gender { get; set; }

    [MapToColumn("address_line_1")]
    public string? AddressLine1 { get; set; }

    [MapToColumn("address_line_2")]
    public string? AddressLine2 { get; set; }

    [MapToColumn("address_line_3")]
    public string? AddressLine3 { get; set; }

    [MapToColumn("address_line_4")]
    public string? AddressLine4 { get; set; }

    [MapToColumn("address_line_5")]
    public string? AddressLine5 { get; set; }

    [MapToColumn("postcode")]
    public string? Postcode { get; set; }

    [MapToColumn("paf_key")]
    public string? PafKey { get; set; }

    [MapToColumn("address_effective_from_date")]
    public string? UsualAddressEffectiveFromDate { get; set; }

    [MapToColumn("reason_for_removal")]
    public string? ReasonForRemoval { get; set; }

    [MapToColumn("reason_for_removal_effective_from_date")]
    public string? ReasonForRemovalEffectiveFromDate { get; set; }

    [MapToColumn("date_of_death")]
    public string? DateOfDeath { get; set; }

    [MapToColumn("death_status")]
    public Int32? DeathStatus { get; set; }

    [MapToColumn("home_telephone_number")]
    public string? TelephoneNumber { get; set; }

    [MapToColumn("home_telephone_effective_from_date")]
    public string? TelephoneNumberEffectiveFromDate { get; set; }

    [MapToColumn("mobile_telephone_number")]
    public string? MobileNumber { get; set; }

    [MapToColumn("mobile_telephone_effective_from_date")]
    public string? MobileNumberEffectiveFromDate { get; set; }

    [MapToColumn("email_address")]
    public string? EmailAddress { get; set; }

    [MapToColumn("email_address_effective_from_date")]
    public string? EmailAddressEffectiveFromDate { get; set; }

    [MapToColumn("preferred_language")]
    public string? PreferredLanguage { get; set; }

    [MapToColumn("is_interpreter_required")]
    public bool? IsInterpreterRequired { get; set; }

    [MapToColumn("invalid_flag")]
    public bool? InvalidFlag { get; set; }

    [MapToColumn("eligibility")]
    public bool? EligibilityFlag { get; set; }
}
