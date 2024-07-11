namespace Model;

using Model.Enums;

public class Participant : BasicParticipantData
{
    public string? PrimaryCareProvider { get; set; }
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingEffectiveFromDate { get; set; }
    public string? PreviousPostingEffectiveFromDate { get; set; }
    public string? NamePrefix { get; set; }
    public string? FirstName { get; set; }
    public string? Surname { get; set; }
    public string? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? Postcode { get; set; }
    public string? ReasonForRemoval { get; set; }
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    public string? DateOfDeath { get; set; }
    public Status? DeathStatus { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? IsInterpreterRequired { get; set; }
    public string? RecordIdentifier { get; set; }
    public string? ParticipantId { get; set; }
    public string? ScreeningId { get; set; }
    public string? BusinessRuleVersion { get; set; }
    public string? ExceptionFlag { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
}
