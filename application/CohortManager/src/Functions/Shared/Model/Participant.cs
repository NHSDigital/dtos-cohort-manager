namespace Model;

using Model.Enums;

public class Participant
{
    public string? RecordType { get; set; }
    public DateTime? ChangeTimeStamp { get; set; }
    public int SerialChangeNumber { get; set; }
    public string NHSId { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public DateTime? PrimaryCareProviderEffectiveFromDate { get; set; }
    public string? CurrentPosting { get; set; }
    public DateTime? CurrentPostingEffectiveFromDate { get; set; }
    public string? PreviousPosting { get; set; }
    public DateTime? PreviousPostingEffectiveFromDate { get; set; }
    public string? NamePrefix { get; set; }
    public string? FirstName { get; set; }
    public string? OtherGivenNames { get; set; }
    public string? Surname { get; set; }
    public string? PreviousSurname { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? Postcode { get; set; }
    public string? PafKey { get; set; }
    public DateTime? UsualAddressEffectiveFromDate { get; set; }
    public string? ReasonForRemoval { get; set; }
    public DateTime? ReasonForRemovalEffectiveFromDate { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public Status? DeathStatus { get; set;}
    public string? TelephoneNumber { get; set; }
    public DateTime? TelephoneNumberEffectiveFromDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? MobileNumberEffectiveFromDate { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime? EmailAddressEffectiveFromDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsInterpreterRequired { get; set; }
    public bool InvalidFlag { get; set; }
    public int? RecordIdentifier { get; set; }
    public string? ChangeReasonCode { get; set; }
}
