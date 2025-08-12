namespace Model;

using Model.Enums;

public class ExceptionDetails
{
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? ParticipantAddressLine1 { get; set; }
    public string? ParticipantAddressLine2 { get; set; }
    public string? ParticipantAddressLine3 { get; set; }
    public string? ParticipantAddressLine4 { get; set; }
    public string? ParticipantAddressLine5 { get; set; }
    public string? ParticipantPostCode { get; set; }
    public string? TelephoneNumberHome { get; set; }
    public string? EmailAddressHome { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public Gender? Gender { get; set; }
    public long? SupersededByNhsNumber { get; set; }
}
