namespace Model;
using ParquetSharp.RowOriented;

public struct ParticipantsParquetMap
{
    [MapToColumn("Record_Type")]
    public string? RecordType;

    [MapToColumn("Change_Time_Stamp")]
    public Double? ChangeTimeStamp;

    [MapToColumn("Serial_Change_Number")]
    public Int64? SerialChangeNumber;

    [MapToColumn("NHS_Number")]
    public Int64? NhsNumber;

    [MapToColumn("Superseded_by_NHS_number")]
    public string? SupersededByNhsNumber;

    [MapToColumn("Primary_Care_Provider")]
    public string? PrimaryCareProvider;

    [MapToColumn("Primary_Care_Provider_Business_Effective_From_Date")]
    public Int64? PrimaryCareProviderEffectiveFromDate;

    [MapToColumn("Current_Posting")]
    public string? CurrentPosting;

    [MapToColumn("Current_Posting_Business_Effective_From_Date")]
    public Int64? CurrentPostingEffectiveFromDate;

    [MapToColumn("Previous_Posting")]
    public string? PreviousPosting;

    [MapToColumn("Previous_Posting_Business_Effective_To_Date")]
    public string? PreviousPostingEffectiveFromDate;

    [MapToColumn("Name_Prefix")]
    public string? NamePrefix;

    [MapToColumn("Given_Name")]
    public string? FirstName;

    [MapToColumn("Other_Given_Name")]
    public string? OtherGivenNames;

    [MapToColumn("Family_Name")]
    public string? SurnamePrefix;

    [MapToColumn("Previous_Family_Name")]
    public string? PreviousSurnamePrefix;

    [MapToColumn("Date_of_Birth")]
    public Int64? DateOfBirth;

    [MapToColumn("Gender")]
    public Int64? Gender;

    [MapToColumn("Address_line_1")]
    public string? AddressLine1;

    [MapToColumn("Address_line_2")]
    public string? AddressLine2;

    [MapToColumn("Address_line_3")]
    public string? AddressLine3;

    [MapToColumn("Address_line_4")]
    public string? AddressLine4;

    [MapToColumn("Address_line_5")]
    public string? AddressLine5;

    [MapToColumn("Postcode")]
    public string? Postcode;

    [MapToColumn("PAF_key")]
    public string? PafKey;

    [MapToColumn("Usual_Address_Business_Effective_From_Date")]
    public Int64? UsualAddressEffectiveFromDate;

    [MapToColumn("Reason_for_Removal")]
    public string? ReasonForRemoval;

    [MapToColumn("Reason_for_Removal_Business_Effective_From_Date")]
    public Int64? ReasonForRemovalEffectiveFromDate ;

    [MapToColumn("Date_of_Death")]
    public string? DateOfDeath;

    [MapToColumn("Death_Status")]
    public string? DeathStatus;

    [MapToColumn("Telephone_Number_Home")]
    public string? TelephoneNumber;

    [MapToColumn("Telephone_Number_Home_Business_Effective_From_Date")]
    public Int64? TelephoneNumberEffectiveFromDate;

    [MapToColumn("Telephone_Number_Mobile")]
    public string? MobileNumber;

    [MapToColumn("Telephone_Number_Mobile_Business_Effective_From_Date")]
    public Int64? MobileNumberEffectiveFromDate;

    [MapToColumn("Email_address_Home")]
    public string? EmailAddress;

    [MapToColumn("Email_address_Home_Business_Effective_From_Date")]
    public Int64? EmailAddressEffectiveFromDate;

    [MapToColumn("Is_Interpreter_Required")]
    public string? IsInterpreterRequired;

    [MapToColumn("Preferred_Language")]
    public string? PreferredLanguage;

    [MapToColumn("Invalid_Flag")]
    public Boolean? InvalidFlag;

    [MapToColumn("Record_Identifier")]
    public string? RecordIdentifier;

    [MapToColumn("Change_Reason_Code")]
    public string? ChangeReasonCode;
}


