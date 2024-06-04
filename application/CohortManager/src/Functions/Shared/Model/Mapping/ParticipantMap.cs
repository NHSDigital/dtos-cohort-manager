namespace Model;

using CsvHelper.Configuration;

public class ParticipantMap : ClassMap<Participant>
{
    public ParticipantMap()
    {
        Map(m => m.RecordType).Name("record_type");
        Map(m => m.ChangeTimeStamp).TypeConverterOption.Format("yyyyMMddHHmmss").Name("change_time_stamp");
        Map(m => m.SerialChangeNumber).Name("serial_change_number");
        Map(m => m.NHSId).Name("nhs_number");
        Map(m => m.SupersededByNhsNumber).Name("superseded_by_nhs_number");
        Map(m => m.PrimaryCareProvider).Name("primary_care_provider");
        Map(m => m.PrimaryCareProviderEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("primary_care_provider_business_effective_from_date");
        Map(m => m.CurrentPosting).Name("current_posting");
        Map(m => m.CurrentPostingEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("current_posting_business_effective_from_date");
        Map(m => m.PreviousPosting).Name("previous_posting");
        Map(m => m.PreviousPostingEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("previous_posting_business_effective_from_date");
        Map(m => m.NamePrefix).Name("name_prefix");
        Map(m => m.FirstName).Name("given_name");
        Map(m => m.OtherGivenNames).Name("other_given_name(s)");
        Map(m => m.Surname).Name("family_name");
        Map(m => m.PreviousSurname).Name("previous_family_name");
        Map(m => m.DateOfBirth).TypeConverterOption.Format(DateFormats.Iso8601).Name("date_of_birth");
        Map(m => m.Gender).Name("gender");
        Map(m => m.AddressLine1).Name("address_line_1");
        Map(m => m.AddressLine2).Name("address_line_2");
        Map(m => m.AddressLine3).Name("address_line_3");
        Map(m => m.AddressLine4).Name("address_line_4");
        Map(m => m.AddressLine5).Name("address_line_5");
        Map(m => m.Postcode).Name("postcode");
        Map(m => m.PafKey).Name("paf_key");
        Map(m => m.UsualAddressEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("usual_address_business_effective_from_date");
        Map(m => m.ReasonForRemoval).Name("reason_for_removal");
        Map(m => m.ReasonForRemovalEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("reason_for_removal_business_effective_from_date");
        Map(m => m.DateOfDeath).TypeConverterOption.Format(DateFormats.Iso8601).Name("date_of_death");
        Map(m => m.DeathStatus).Name("death_status");
        Map(m => m.TelephoneNumber).Name("telephone_number(home)");
        Map(m => m.TelephoneNumberEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("telephone_number(home)_business_effective_from_date");
        Map(m => m.MobileNumber).Name("telephone_number(mobile)");
        Map(m => m.MobileNumberEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("telephone_number(mobile)_business_effective_from_date");
        Map(m => m.EmailAddress).Name("email_address(home)");
        Map(m => m.EmailAddressEffectiveFromDate).TypeConverterOption.Format(DateFormats.Iso8601).Name("email_address(home)_business_effective_from_date");
        Map(m => m.PreferredLanguage).Name("preferred_language");
        Map(m => m.IsInterpreterRequired).Name("is_interpreter_required");
        Map(m => m.InvalidFlag).Name("invalid_flag");
        Map(m => m.RecordIdentifier).Name("record_identifier");
        Map(m => m.ChangeReasonCode).Name("change_reason_code");
    }
}
