namespace Model;

using ParquetSharp.RowOriented;
using Parquet.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

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

    public static ParticipantsParquet ToParticipantParquet(ParticipantsParquetMap participantsParquetMap)
    {
        return new ParticipantsParquet()
        {
            record_type = participantsParquetMap.RecordType,
            change_time_stamp = participantsParquetMap.ChangeTimeStamp,
            serial_change_number = participantsParquetMap.SerialChangeNumber,
            nhs_number = participantsParquetMap.NhsNumber,
            superseded_by_nhs_number = participantsParquetMap.SupersededByNhsNumber,
            primary_care_provider = participantsParquetMap.PrimaryCareProvider,
            primary_care_effective_from_date = participantsParquetMap.PrimaryCareEffectiveFromDate,
            current_posting = participantsParquetMap.CurrentPosting,
            current_posting_effective_from_date = participantsParquetMap.CurrentPostingEffectiveFromDate,
            name_prefix = participantsParquetMap.NamePrefix,
            given_name = participantsParquetMap.FirstName,
            other_given_name = participantsParquetMap.OtherGivenNames,
            family_name = participantsParquetMap.SurnamePrefix,
            previous_family_name = participantsParquetMap.PreviousSurnamePrefix,
            date_of_birth = participantsParquetMap.DateOfBirth,
            gender = participantsParquetMap.Gender,
            address_line_1 = participantsParquetMap.AddressLine1,
            address_line_2 = participantsParquetMap.AddressLine2,
            address_line_3 = participantsParquetMap.AddressLine3,
            address_line_4 = participantsParquetMap.AddressLine4,
            address_line_5 = participantsParquetMap.AddressLine5,
            postcode = participantsParquetMap.Postcode,
            paf_key = participantsParquetMap.PafKey,
            address_effective_from_date = participantsParquetMap.UsualAddressEffectiveFromDate,
            reason_for_removal = participantsParquetMap.ReasonForRemoval,
            reason_for_removal_effective_from_date = participantsParquetMap.ReasonForRemovalEffectiveFromDate,
            date_of_death = participantsParquetMap.DateOfDeath,
            death_status = participantsParquetMap.DeathStatus,
            home_telephone_number = participantsParquetMap.TelephoneNumber,
            home_telephone_effective_from_date = participantsParquetMap.TelephoneNumberEffectiveFromDate,
            mobile_telephone_number = participantsParquetMap.MobileNumber,
            mobile_telephone_effective_from_date = participantsParquetMap.MobileNumberEffectiveFromDate,
            email_address = participantsParquetMap.EmailAddress,
            email_address_effective_from_date = participantsParquetMap.EmailAddressEffectiveFromDate,
            preferred_language = participantsParquetMap.PreferredLanguage,
            is_interpreter_required = participantsParquetMap.IsInterpreterRequired,
            invalid_flag = participantsParquetMap.InvalidFlag,
            eligibility = participantsParquetMap.EligibilityFlag
        };
    }
}
