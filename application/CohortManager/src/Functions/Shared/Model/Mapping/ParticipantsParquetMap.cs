namespace Model;
using ParquetSharp.RowOriented;

public struct ParticipantsParquetMap
{
    [MapToColumn("record_type")]
    public string? RecordType;

    [MapToColumn("change_time_stamp")]
    public Int64? ChangeTimeStamp;

    [MapToColumn("serial_change_number")]
    public Int64? SerialChangeNumber;

    [MapToColumn("nhs_number")]
    public Int64? NhsNumber;

    [MapToColumn("superseded_by_nhs_number")]
    public Int32? SupersededByNhsNumber;

    [MapToColumn("primary_care_provider")]
    public string? PrimaryCareProvider;

    [MapToColumn("primary_care_effective_from_date")]
    public string? PrimaryCareEffectiveFromDate;

    [MapToColumn("current_posting")]
    public string? CurrentPosting;

    [MapToColumn("current_posting_effective_from_date")]
    public string? CurrentPostingEffectiveFromDate;

    [MapToColumn("name_prefix")]
    public string? NamePrefix;

    [MapToColumn("given_name")]
    public string? FirstName;

    [MapToColumn("other_given_name")]
    public string? OtherGivenNames;

    [MapToColumn("family_name")]
    public string? SurnamePrefix;

    [MapToColumn("previous_family_name")]
    public string? PreviousSurnamePrefix;

    [MapToColumn("date_of_birth")]
    public string? DateOfBirth;

    [MapToColumn("gender")]
    public Int64? Gender;

    [MapToColumn("address_line_1")]
    public string? AddressLine1;

    [MapToColumn("address_line_2")]
    public string? AddressLine2;

    [MapToColumn("address_line_3")]
    public string? AddressLine3;

    [MapToColumn("address_line_4")]
    public string? AddressLine4;

    [MapToColumn("address_line_5")]
    public string? AddressLine5;

    [MapToColumn("postcode")]
    public string? Postcode;

    [MapToColumn("paf_key")]
    public string? PafKey;

    [MapToColumn("address_effective_from_date")]
    public string? UsualAddressEffectiveFromDate;

    [MapToColumn("reason_for_removal")]
    public string? ReasonForRemoval;

    [MapToColumn("reason_for_removal_effective_from_date")]
    public string? ReasonForRemovalEffectiveFromDate;

    [MapToColumn("date_of_death")]
    public string? DateOfDeath;

    [MapToColumn("death_status")]
    public Int32? DeathStatus;

    [MapToColumn("home_telephone_number")]
    public string? TelephoneNumber;

    [MapToColumn("home_telephone_effective_from_date")]
    public string? TelephoneNumberEffectiveFromDate;

    [MapToColumn("mobile_telephone_number")]
    public string? MobileNumber;

    [MapToColumn("mobile_telephone_effective_from_date")]
    public string? MobileNumberEffectiveFromDate;

    [MapToColumn("email_address")]
    public string? EmailAddress;

    [MapToColumn("email_address_effective_from_date")]
    public string? EmailAddressEffectiveFromDate;

    [MapToColumn("is_interpreter_required")]
    public string? IsInterpreterRequired;

    [MapToColumn("preferred_language")]
    public string? PreferredLanguage;

    [MapToColumn("invalid_flag")]
    public Boolean? InvalidFlag;
}


