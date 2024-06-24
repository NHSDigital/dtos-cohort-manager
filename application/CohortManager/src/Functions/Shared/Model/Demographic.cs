namespace Model;


public class Demographic
{
    public string? ParticipantId { get; set; }
    public string? NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? PrimaryCareProviderFromDate { get; set; }
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingFromDate { get; set; }
    public string? PreviousPosting { get; set; }
    public string? PreviousPostingToDate { get; set; }
    public string? NamePrefix { get; set; }
    public string? GivenName { get; set; }
    public string? OtherGivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? PreviousFamilyName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? PostCode { get; set; }
    public string? PafKey { get; set; }
    public string? UsualAddressFromDate { get; set; }
    public string? DateOfDeath { get; set; }
    public string? DeathStatus { get; set; }
    public string? TelephoneNumberHome { get; set; }
    public string? TelephoneNumberHomeFromDate { get; set; }
    public string? TelephoneNumberMobile { get; set; }
    public string? TelephoneNumberMobileFromDate { get; set; }
    public string? EmailAddressHome { get; set; }
    public string? EmailAddressHomeFromDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? InterpreterRequired { get; set; }
    public string? InvalidFlag { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
}
