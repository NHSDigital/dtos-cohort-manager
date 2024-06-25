namespace Model;

using CsvHelper.Configuration;

public class ParticipantMap : ClassMap<Participant>
{
    public ParticipantMap()
    {
        Map(m => m.RecordType).Name("Record Type");
        Map(m => m.ChangeTimeStamp).TypeConverterOption.Format("yyyyMMddHHmmss").Name("Change Time Stamp");
        Map(m => m.SerialChangeNumber).Name("Serial Change Number");
        Map(m => m.NhsNumber).Name("NHS Number");
        Map(m => m.SupersededByNhsNumber).Name("Superseded by NHS number");
        Map(m => m.PrimaryCareProvider).Name("Primary Care Provider");
        Map(m => m.PrimaryCareProviderEffectiveFromDate).Name("Primary Care Provider Business Effective From Date");
        Map(m => m.CurrentPosting).Name("Current Posting");
        Map(m => m.CurrentPostingEffectiveFromDate).Name("Current Posting Business Effective From Date");
        Map(m => m.PreviousPosting).Name("Previous Posting");
        Map(m => m.PreviousPostingEffectiveFromDate).Name("Previous Posting Business Effective To Date");
        Map(m => m.NamePrefix).Name("Name Prefix");
        Map(m => m.FirstName).Name("Given Name");
        Map(m => m.OtherGivenNames).Name("Other Given Name(s)");
        Map(m => m.Surname).Name("Family Name");
        Map(m => m.PreviousSurname).Name("Previous Family Name");
        Map(m => m.DateOfBirth).Name("Date of Birth");
        Map(m => m.Gender).Name("Gender");
        Map(m => m.AddressLine1).Name("Address line 1");
        Map(m => m.AddressLine2).Name("Address line 2");
        Map(m => m.AddressLine3).Name("Address line 3");
        Map(m => m.AddressLine4).Name("Address line 4");
        Map(m => m.AddressLine5).Name("Address line 5");
        Map(m => m.Postcode).Name("Postcode");
        Map(m => m.PafKey).Name("PAF key");
        Map(m => m.UsualAddressEffectiveFromDate).Name("Usual Address Business Effective From Date");
        Map(m => m.ReasonForRemoval).Name("Reason for Removal");
        Map(m => m.ReasonForRemovalEffectiveFromDate).Name("Reason for Removal Business Effective From Date");
        Map(m => m.DateOfDeath).Name("Date of Death");
        Map(m => m.DeathStatus).Name("Death Status");
        Map(m => m.TelephoneNumber).Name("Telephone Number (Home)");
        Map(m => m.TelephoneNumberEffectiveFromDate).Name("Telephone Number (Home) Business Effective From Date");
        Map(m => m.MobileNumber).Name("Telephone Number (Mobile)");
        Map(m => m.MobileNumberEffectiveFromDate).Name("Telephone Number (Mobile) Business Effective From Date");
        Map(m => m.EmailAddress).Name("E-mail address (Home)");
        Map(m => m.EmailAddressEffectiveFromDate).Name("E-mail address (Home) Business Effective From Date");
        Map(m => m.PreferredLanguage).Name("Preferred Language");
        Map(m => m.IsInterpreterRequired).Name("Interpreter required");
        Map(m => m.InvalidFlag).Name("Invalid Flag");
        Map(m => m.RecordIdentifier).Name("Record Identifier");
        Map(m => m.ChangeReasonCode).Name("Change Reason Code");
    }
}
