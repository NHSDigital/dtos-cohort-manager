namespace Model;

using Model.Enums;
using System;
using System.Globalization;
using NHS.CohortManager.Shared.Utilities;

public class Participant
{
    public Participant() { }
    public Participant(ParticipantManagement pm)
    {
        if (pm == null)
        {
            return;
        }

        ParticipantId = pm.ParticipantId.ToString();
        ScreeningId = pm.ScreeningId.ToString();
        NhsNumber = pm.NHSNumber.ToString();
        RecordType = pm.RecordType;
        EligibilityFlag = pm.EligibilityFlag.ToString();
        ReasonForRemoval = pm.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = pm.ReasonForRemovalDate.ToString();
        BusinessRuleVersion = pm.BusinessRuleVersion;
        ExceptionFlag = pm.ExceptionFlag.ToString();
        BlockedFlag = pm.BlockedFlag.ToString();
        ReferralFlag = pm.ReferralFlag.ToString();
        RecordInsertDateTime = pm.RecordInsertDateTime.ToString();
        RecordUpdateDateTime = pm.RecordUpdateDateTime.ToString();

    }

    public Participant(CohortDistributionParticipant cohortDistributionParticipant)
    {
        RecordType = cohortDistributionParticipant.RecordType;
        ParticipantId = cohortDistributionParticipant.ParticipantId;
        NhsNumber = cohortDistributionParticipant.NhsNumber;
        SupersededByNhsNumber = cohortDistributionParticipant.SupersededByNhsNumber;
        PrimaryCareProvider = cohortDistributionParticipant.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = cohortDistributionParticipant.PrimaryCareProviderEffectiveFromDate;
        NamePrefix = cohortDistributionParticipant.NamePrefix;
        FirstName = cohortDistributionParticipant.FirstName;
        OtherGivenNames = cohortDistributionParticipant.OtherGivenNames;
        FamilyName = cohortDistributionParticipant.FamilyName;
        PreviousFamilyName = cohortDistributionParticipant.PreviousFamilyName;
        DateOfBirth = cohortDistributionParticipant.DateOfBirth;
        Gender = cohortDistributionParticipant.Gender.GetValueOrDefault();
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
        ScreeningAcronym = cohortDistributionParticipant.ScreeningAcronym;
        ScreeningName = cohortDistributionParticipant.ScreeningName;
        ScreeningId = cohortDistributionParticipant.ScreeningServiceId;
        CurrentPosting = cohortDistributionParticipant.CurrentPosting;
        CurrentPostingEffectiveFromDate = cohortDistributionParticipant.CurrentPostingEffectiveFromDate;
        EligibilityFlag = cohortDistributionParticipant.EligibilityFlag;
        InvalidFlag = cohortDistributionParticipant.InvalidFlag;
        ReferralFlag = cohortDistributionParticipant.ReferralFlag.ToString();
    }

    public Participant(PdsDemographic pdsDemographic)
    {
        RecordType = Actions.Amended; // TODO this does not exist on PdsDemographic, so is hardcoded for the time being.
        ParticipantId = null; // TODO this will never exist on PdsDemographic, so is hardcoded to null.
        NhsNumber = pdsDemographic.NhsNumber;
        SupersededByNhsNumber = pdsDemographic.SupersededByNhsNumber;
        PrimaryCareProvider = pdsDemographic.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = pdsDemographic.PrimaryCareProviderEffectiveFromDate;
        NamePrefix = pdsDemographic.NamePrefix;
        FirstName = pdsDemographic.FirstName;
        OtherGivenNames = pdsDemographic.OtherGivenNames;
        FamilyName = pdsDemographic.FamilyName;
        PreviousFamilyName = pdsDemographic.PreviousFamilyName;
        DateOfBirth = pdsDemographic.DateOfBirth;
        Gender = pdsDemographic.Gender.GetValueOrDefault();
        AddressLine1 = pdsDemographic.AddressLine1;
        AddressLine2 = pdsDemographic.AddressLine2;
        AddressLine3 = pdsDemographic.AddressLine3;
        AddressLine4 = pdsDemographic.AddressLine4;
        AddressLine5 = pdsDemographic.AddressLine5;
        Postcode = pdsDemographic.Postcode;
        UsualAddressEffectiveFromDate = pdsDemographic.UsualAddressEffectiveFromDate;
        DateOfDeath = pdsDemographic.DateOfDeath;
        TelephoneNumber = pdsDemographic.TelephoneNumber;
        TelephoneNumberEffectiveFromDate = pdsDemographic.TelephoneNumberEffectiveFromDate;
        MobileNumber = pdsDemographic.MobileNumber;
        MobileNumberEffectiveFromDate = pdsDemographic.MobileNumberEffectiveFromDate;
        EmailAddress = pdsDemographic.EmailAddress;
        EmailAddressEffectiveFromDate = pdsDemographic.EmailAddressEffectiveFromDate;
        PreferredLanguage = pdsDemographic.PreferredLanguage;
        IsInterpreterRequired = pdsDemographic.IsInterpreterRequired == "True" ? "1" : "0";
        ReasonForRemoval = pdsDemographic.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = pdsDemographic.RemovalEffectiveFromDate;
        RecordInsertDateTime = pdsDemographic.RecordInsertDateTime;
        RecordUpdateDateTime = pdsDemographic.RecordUpdateDateTime;
        ScreeningAcronym = "BSS"; // TODO this does not exist on PdsDemographic, so is hardcoded for the time being.
        ScreeningName = "Breast Screening"; // TODO this does not exist on PdsDemographic, so is hardcoded for the time being.
        ScreeningId = "1"; // TODO this does not exist on PdsDemographic, so is hardcoded for the time being.
        CurrentPosting = pdsDemographic.CurrentPosting;
        EligibilityFlag = "0"; // TODO this does not exist on PdsDemographic, so is hardcoded for the time being.
    }

    public ParticipantDemographic ToParticipantDemographic()
    {
        return new ParticipantDemographic
        {
            NhsNumber = !string.IsNullOrEmpty(NhsNumber) ? long.Parse(NhsNumber) : throw new FormatException("Cannot parse nhs number to Long"),
            SupersededByNhsNumber = !string.IsNullOrEmpty(SupersededByNhsNumber) ? long.Parse(SupersededByNhsNumber) : null,
            PrimaryCareProvider = PrimaryCareProvider,
            PrimaryCareProviderFromDate = PrimaryCareProviderEffectiveFromDate,
            CurrentPosting = CurrentPosting,
            CurrentPostingFromDate = CurrentPostingEffectiveFromDate,
            NamePrefix = NamePrefix,
            GivenName = FirstName,
            OtherGivenName = OtherGivenNames,
            FamilyName = FamilyName,
            PreviousFamilyName = PreviousFamilyName,
            DateOfBirth = DateOfBirth,
            Gender = Gender.HasValue ? (short?)Gender : null,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            PostCode = Postcode,
            PafKey = PafKey,
            UsualAddressFromDate = UsualAddressEffectiveFromDate,
            DateOfDeath = DateOfDeath,
            DeathStatus = DeathStatus.HasValue ? (short?)DeathStatus : null,
            TelephoneNumberHome = TelephoneNumber,
            TelephoneNumberHomeFromDate = TelephoneNumberEffectiveFromDate,
            TelephoneNumberMob = MobileNumber,
            TelephoneNumberMobFromDate = MobileNumberEffectiveFromDate,
            EmailAddressHome = EmailAddress,
            EmailAddressHomeFromDate = EmailAddressEffectiveFromDate,
            PreferredLanguage = PreferredLanguage,
            InterpreterRequired = !string.IsNullOrEmpty(IsInterpreterRequired) ? short.Parse(IsInterpreterRequired) : null,
            InvalidFlag = (short?)GetInvalidFlag(),
            RecordInsertDateTime = !string.IsNullOrEmpty(RecordInsertDateTime) ? MappingUtilities.ParseDates(RecordInsertDateTime) : DateTime.UtcNow,
            RecordUpdateDateTime = !string.IsNullOrEmpty(RecordUpdateDateTime) ? MappingUtilities.ParseDates(RecordUpdateDateTime) : null
        };
    }

    public ParticipantManagement ToParticipantManagement()
    {
        var participantManagement = new ParticipantManagement
        {
            ScreeningId = long.Parse(ScreeningId!),
            NHSNumber = long.Parse(NhsNumber!),
            RecordType = RecordType!,
            EligibilityFlag = MappingUtilities.ParseStringFlag(EligibilityFlag ?? "1"),
            ReasonForRemoval = ReasonForRemoval,
            ReasonForRemovalDate = MappingUtilities.ParseDates(ReasonForRemovalEffectiveFromDate!),
            BusinessRuleVersion = BusinessRuleVersion,
            ExceptionFlag = MappingUtilities.ParseStringFlag(ExceptionFlag ?? "0"),
            BlockedFlag = MappingUtilities.ParseStringFlag(BlockedFlag ?? "0"),
            ReferralFlag = MappingUtilities.ParseStringFlag(ReferralFlag ?? "0"),
            RecordInsertDateTime = MappingUtilities.ParseDates(RecordInsertDateTime!),
            RecordUpdateDateTime = MappingUtilities.ParseDates(RecordUpdateDateTime!),
        };

        if (ParticipantId != null)
            participantManagement.ParticipantId = long.Parse(ParticipantId);

        return participantManagement;
    }

    public ParticipantManagement ToParticipantManagement(ParticipantManagement existingRecord)
    {
        var participantManagement = new ParticipantManagement
        {
            ParticipantId = existingRecord.ParticipantId,
            ScreeningId = long.Parse(ScreeningId),
            NHSNumber = long.Parse(NhsNumber),
            RecordType = RecordType,
            EligibilityFlag = MappingUtilities.ParseStringFlag(EligibilityFlag ?? "1"),
            ReasonForRemoval = ReasonForRemoval,
            ReasonForRemovalDate = MappingUtilities.ParseDates(ReasonForRemovalEffectiveFromDate),
            BusinessRuleVersion = BusinessRuleVersion,
            ExceptionFlag = MappingUtilities.ParseStringFlag(ExceptionFlag ?? "0"),
            BlockedFlag = MappingUtilities.ParseStringFlag(BlockedFlag ?? "0"),
            ReferralFlag = existingRecord.ReferralFlag,
            RecordInsertDateTime = existingRecord.RecordInsertDateTime,
            RecordUpdateDateTime = existingRecord.RecordUpdateDateTime,
            IsHigherRisk = existingRecord.IsHigherRisk
        };

        if (ParticipantId != null)
            participantManagement.ParticipantId = long.Parse(ParticipantId);

        return participantManagement;
    }

    private int GetInvalidFlag()
    {
        int result = 0;
        if (!string.IsNullOrEmpty(InvalidFlag))
        {
            result = InvalidFlag.Equals("true", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
        }
        return result;
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
    public string? NamePrefix { get; set; }
    public string? FirstName { get; set; }
    public string? OtherGivenNames { get; set; }
    public string? FamilyName { get; set; }
    public string? PreviousFamilyName { get; set; }
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
    public string? ParticipantId { get; set; }
    public string? ScreeningId { get; set; }
    public string? BusinessRuleVersion { get; set; }
    public string? ExceptionFlag { get; set; }
    public string? BlockedFlag { get; set; }
    public string? ReferralFlag { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
    public string? ScreeningAcronym { get; set; }
    public string? ScreeningName { get; set; }
    public string? EligibilityFlag { get; set; }
}
