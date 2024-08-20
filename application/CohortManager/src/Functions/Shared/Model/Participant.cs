namespace Model;

using Model.Enums;

public class Participant
{
    public Participant() {}
    public Participant(CohortDistributionParticipant cohortDistributionParticipant)
    {
        ParticipantId = cohortDistributionParticipant.ParticipantId;
        NhsNumber = cohortDistributionParticipant.NhsNumber;
        SupersededByNhsNumber = cohortDistributionParticipant.SupersededByNhsNumber;
        PrimaryCareProvider = cohortDistributionParticipant.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = cohortDistributionParticipant.PrimaryCareProviderEffectiveFromDate;
        NamePrefix = cohortDistributionParticipant.NamePrefix;
        FirstName = cohortDistributionParticipant.FirstName;
        OtherGivenNames = cohortDistributionParticipant.OtherGivenNames;
        Surname = cohortDistributionParticipant.Surname;
        PreviousSurname = cohortDistributionParticipant.PreviousSurname;
        DateOfBirth = cohortDistributionParticipant.DateOfBirth;
        Gender = cohortDistributionParticipant.Gender;
        AddressLine1 = cohortDistributionParticipant.AddressLine1;
        AddressLine2 = cohortDistributionParticipant.AddressLine2;
        AddressLine3 = cohortDistributionParticipant.AddressLine3;
        AddressLine4 = cohortDistributionParticipant.AddressLine4;
        AddressLine5 = cohortDistributionParticipant.AddressLine5;
        Postcode = cohortDistributionParticipant.Postcode;
        UsualAddressEffectiveFromDate = cohortDistributionParticipant.UsualAddressEffectiveFromDate;
        DateOfDeath = cohortDistributionParticipant.DateOfDeath;
        TelephoneNumber = cohortDistributionParticipant.TelephoneNumber;
        TelephoneNumberEffectiveFromDate = cohortDistributionParticipant.TelephoneNumberEffectiveFromDate;
        MobileNumber = cohortDistributionParticipant.MobileNumber;
        MobileNumberEffectiveFromDate = cohortDistributionParticipant.MobileNumberEffectiveFromDate;
        EmailAddress = cohortDistributionParticipant.EmailAddress;
        EmailAddressEffectiveFromDate = cohortDistributionParticipant.EmailAddressEffectiveFromDate;
        PreferredLanguage = cohortDistributionParticipant.PreferredLanguage;
        IsInterpreterRequired = cohortDistributionParticipant.IsInterpreterRequired;
        ReasonForRemoval = cohortDistributionParticipant.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = cohortDistributionParticipant.ReasonForRemovalEffectiveFromDate;
        RecordInsertDateTime = cohortDistributionParticipant.RecordInsertDateTime;
        RecordUpdateDateTime = cohortDistributionParticipant.RecordUpdateDateTime;
    }

    public string? RecordType { get; set; }
    public string? ChangeTimeStamp { get; set; }
    public string? SerialChangeNumber { get; set; }
    public string? NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingEffectiveFromDate { get; set; }
    public string? PreviousPosting { get; set; }
    public string? PreviousPostingEffectiveFromDate { get; set; }
    public string? NamePrefix { get; set; }
    public string? FirstName { get; set; }
    public string? OtherGivenNames { get; set; }
    public string? Surname { get; set; }
    public string? PreviousSurname { get; set; }
    public string? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? Postcode { get; set; }
    public string? PafKey { get; set; }
    public string? UsualAddressEffectiveFromDate { get; set; }
    public string? ReasonForRemoval { get; set; }
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    public string? DateOfDeath { get; set; }
    public Status? DeathStatus { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? MobileNumberEffectiveFromDate { get; set; }
    public string? EmailAddress { get; set; }
    public string? EmailAddressEffectiveFromDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? IsInterpreterRequired { get; set; }
    public string? InvalidFlag { get; set; }
    public string? RecordIdentifier { get; set; }
    public string? ChangeReasonCode { get; set; }
    public string? ParticipantId { get; set; }
    public string? ScreeningId { get; set; }
    public string? BusinessRuleVersion { get; set; }
    public string? ExceptionFlag { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
    public string? ScreeningAcronym { get; set; }
    public string? ScreeningName { get; set; }
}
