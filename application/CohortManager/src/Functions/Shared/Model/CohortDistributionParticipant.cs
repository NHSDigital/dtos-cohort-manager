namespace Model;

using Enums;
using NHS.CohortManager.Shared.Utilities;

public class CohortDistributionParticipant
{
    public CohortDistributionParticipant()
    {
        NhsNumber = null!;
        RecordType = null!;
    }

    public CohortDistributionParticipant(CohortDistribution cohortDistribution)
    {
        RequestId = cohortDistribution!.RequestId.ToString();
        NhsNumber = cohortDistribution.NHSNumber.ToString();
        SupersededByNhsNumber = cohortDistribution.SupersededNHSNumber.ToString() ?? null;
        PrimaryCareProvider = cohortDistribution.PrimaryCareProvider ?? null;
        PrimaryCareProviderEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.PrimaryCareProviderDate);
        NamePrefix = cohortDistribution.NamePrefix;
        FirstName = cohortDistribution.GivenName;
        OtherGivenNames = cohortDistribution.OtherGivenName;
        FamilyName = cohortDistribution.FamilyName;
        PreviousFamilyName = cohortDistribution.PreviousFamilyName;
        DateOfBirth = MappingUtilities.FormatDateTime(cohortDistribution.DateOfBirth);
        Gender = (Gender?)cohortDistribution.Gender;
        AddressLine1 = cohortDistribution.AddressLine1;
        AddressLine2 = cohortDistribution.AddressLine2;
        AddressLine3 = cohortDistribution.AddressLine3;
        AddressLine4 = cohortDistribution.AddressLine4;
        AddressLine5 = cohortDistribution.AddressLine5;
        Postcode = cohortDistribution.PostCode;
        UsualAddressEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.UsualAddressFromDt);
        DateOfDeath = MappingUtilities.FormatDateTime(cohortDistribution.DateOfDeath);
        TelephoneNumber = cohortDistribution.TelephoneNumberHome;
        TelephoneNumberEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.TelephoneNumberHomeFromDt);
        MobileNumber = cohortDistribution.TelephoneNumberMob;
        MobileNumberEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.TelephoneNumberMobFromDt);
        EmailAddress = cohortDistribution.EmailAddressHome;
        EmailAddressEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.EmailAddressHomeFromDt);
        PreferredLanguage = cohortDistribution.PreferredLanguage;
        IsInterpreterRequired = cohortDistribution.InterpreterRequired == 1 ? "1" : "0";
        ReasonForRemoval = cohortDistribution.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.ReasonForRemovalDate);
        Extracted = cohortDistribution.IsExtracted == 1 ? "1" : "0";
        RecordInsertDateTime = MappingUtilities.FormatDateTime(cohortDistribution.RecordInsertDateTime);
        RecordUpdateDateTime = MappingUtilities.FormatDateTime(cohortDistribution.RecordUpdateDateTime);
        CurrentPosting = cohortDistribution.CurrentPosting;
        CurrentPostingEffectiveFromDate = MappingUtilities.FormatDateTime(cohortDistribution.CurrentPostingFromDt);
        ParticipantId = cohortDistribution.ParticipantId.ToString();
        // record type is not actually in the database but we need to set it as it's not nullable
        RecordType = null!;
    }

    public CohortDistributionParticipant(ParticipantManagement participant, Demographic demographic)
    {
        ParticipantId = participant.ParticipantId.ToString();
        NhsNumber = participant.NHSNumber.ToString();
        SupersededByNhsNumber = demographic.SupersededByNhsNumber;
        PrimaryCareProvider = demographic.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = demographic.PrimaryCareProviderEffectiveFromDate;
        NamePrefix = demographic.NamePrefix;
        FirstName = demographic.FirstName;
        OtherGivenNames = demographic.OtherGivenNames;
        FamilyName = demographic.FamilyName;
        PreviousFamilyName = demographic.PreviousFamilyName;
        DateOfBirth = demographic.DateOfBirth;
        Gender = demographic.Gender.GetValueOrDefault();
        AddressLine1 = demographic.AddressLine1;
        AddressLine2 = demographic.AddressLine2;
        AddressLine3 = demographic.AddressLine3;
        AddressLine4 = demographic.AddressLine4;
        AddressLine5 = demographic.AddressLine5;
        Postcode = demographic.Postcode;
        UsualAddressEffectiveFromDate = demographic.UsualAddressEffectiveFromDate;
        DateOfDeath = demographic.DateOfDeath;
        TelephoneNumber = demographic.TelephoneNumber;
        TelephoneNumberEffectiveFromDate = demographic.TelephoneNumberEffectiveFromDate;
        MobileNumber = demographic.MobileNumber;
        MobileNumberEffectiveFromDate = demographic.MobileNumberEffectiveFromDate;
        EmailAddress = demographic.EmailAddress;
        EmailAddressEffectiveFromDate = demographic.EmailAddressEffectiveFromDate;
        PreferredLanguage = demographic.PreferredLanguage;
        IsInterpreterRequired = demographic.IsInterpreterRequired;
        ReasonForRemoval = participant.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = participant.ReasonForRemovalDate.ToString();
        RecordInsertDateTime = demographic.RecordInsertDateTime;
        RecordUpdateDateTime = participant.RecordUpdateDateTime.ToString();
        ScreeningServiceId = participant.ScreeningId.ToString();
        Extracted = null;
        RecordType = participant.RecordType;
        CurrentPosting = demographic.CurrentPosting;
        CurrentPostingEffectiveFromDate = demographic.CurrentPostingEffectiveFromDate;
        ExceptionFlag = participant.ExceptionFlag;
        InvalidFlag = demographic.InvalidFlag;
    }

    public CohortDistributionParticipant(ParticipantManagement participant, ParticipantDemographic demographic)
    {
        ParticipantId = participant.ParticipantId.ToString();
        NhsNumber = participant.NHSNumber.ToString();
        SupersededByNhsNumber = demographic.SupersededByNhsNumber.ToString();
        PrimaryCareProvider = demographic.PrimaryCareProvider;
        PrimaryCareProviderEffectiveFromDate = demographic.PrimaryCareProviderFromDate;
        NamePrefix = demographic.NamePrefix;
        FirstName = demographic.GivenName;
        OtherGivenNames = demographic.OtherGivenName;
        FamilyName = demographic.FamilyName;
        PreviousFamilyName = demographic.PreviousFamilyName;
        DateOfBirth = demographic.DateOfBirth;
        Gender = (Gender) (demographic.Gender ?? 0);
        AddressLine1 = demographic.AddressLine1;
        AddressLine2 = demographic.AddressLine2;
        AddressLine3 = demographic.AddressLine3;
        AddressLine4 = demographic.AddressLine4;
        AddressLine5 = demographic.AddressLine5;
        Postcode = demographic.PostCode;
        UsualAddressEffectiveFromDate = demographic.UsualAddressFromDate;
        DateOfDeath = demographic.DateOfDeath;
        TelephoneNumber = demographic.TelephoneNumberHome;
        TelephoneNumberEffectiveFromDate = demographic.TelephoneNumberHomeFromDate;
        MobileNumber = demographic.TelephoneNumberMob;
        MobileNumberEffectiveFromDate = demographic.TelephoneNumberMobFromDate;
        EmailAddress = demographic.EmailAddressHome;
        EmailAddressEffectiveFromDate = demographic.EmailAddressHomeFromDate;
        PreferredLanguage = demographic.PreferredLanguage;
        IsInterpreterRequired = demographic.InterpreterRequired.ToString();
        ReasonForRemoval = participant.ReasonForRemoval;
        ReasonForRemovalEffectiveFromDate = participant.ReasonForRemovalDate.ToString();
        ScreeningServiceId = participant.ScreeningId.ToString();
        Extracted = null;
        RecordType = participant.RecordType;
        CurrentPosting = demographic.CurrentPosting;
        CurrentPostingEffectiveFromDate = demographic.CurrentPostingFromDate;
        ExceptionFlag = participant.ExceptionFlag;
        InvalidFlag = demographic.InvalidFlag.ToString();
        EligibilityFlag = participant.EligibilityFlag.ToString();
    }

    public string? RequestId { get; set; }
    public string NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
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
    public string? UsualAddressEffectiveFromDate { get; set; }
    public string? DateOfDeath { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? MobileNumberEffectiveFromDate { get; set; }
    public string? EmailAddress { get; set; }
    public string? EmailAddressEffectiveFromDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? IsInterpreterRequired { get; set; }
    public string? ReasonForRemoval { get; set; }
    public string? ReasonForRemovalEffectiveFromDate { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
    public string? Extracted { get; set; }
    public string? ScreeningAcronym { get; set; }
    public string? ScreeningServiceId { get; set; }
    public string? ScreeningName { get; set; }
    public string? EligibilityFlag { get; set; }
    public short? ExceptionFlag { get; set; }
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingEffectiveFromDate { get; set; }
    public string? ParticipantId { get; set; }
    public string RecordType { get; set; }
    public string? InvalidFlag { get; set; }


    public CohortDistribution ToCohortDistribution()
    {
        var cohortDis = new CohortDistribution()
        {
            RequestId = GetRequestId(),
            NHSNumber = long.Parse(NhsNumber),
            SupersededNHSNumber = long.TryParse(SupersededByNhsNumber, out var supNhsNum) ? supNhsNum : null,
            PrimaryCareProvider = PrimaryCareProvider ?? null,
            PrimaryCareProviderDate = MappingUtilities.ParseDates(PrimaryCareProviderEffectiveFromDate),
            NamePrefix = NamePrefix,
            GivenName = FirstName,
            OtherGivenName = OtherGivenNames,
            FamilyName = FamilyName,
            PreviousFamilyName = PreviousFamilyName,
            DateOfBirth = MappingUtilities.ParseDates(DateOfBirth),
            Gender = (short?)Gender ?? 0,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            PostCode = Postcode,
            UsualAddressFromDt = MappingUtilities.ParseDates(UsualAddressEffectiveFromDate),
            DateOfDeath = MappingUtilities.ParseDates(DateOfDeath),
            TelephoneNumberHome = TelephoneNumber,
            TelephoneNumberHomeFromDt = MappingUtilities.ParseDates(TelephoneNumberEffectiveFromDate),
            TelephoneNumberMob = MobileNumber,
            TelephoneNumberMobFromDt = MappingUtilities.ParseDates(MobileNumberEffectiveFromDate),
            EmailAddressHome = EmailAddress,
            EmailAddressHomeFromDt = MappingUtilities.ParseDates(EmailAddressEffectiveFromDate),
            PreferredLanguage = PreferredLanguage,
            InterpreterRequired = short.TryParse(IsInterpreterRequired, out var interpreter) ? interpreter : (short)0,
            ReasonForRemoval = ReasonForRemoval,
            ReasonForRemovalDate = MappingUtilities.ParseDates(ReasonForRemovalEffectiveFromDate),
            IsExtracted = short.TryParse(Extracted, out var extracted) ? extracted : (short)0,
            RecordInsertDateTime = MappingUtilities.ParseDates(RecordInsertDateTime),
            RecordUpdateDateTime = MappingUtilities.ParseDates(RecordUpdateDateTime),
            CurrentPosting = CurrentPosting,
            CurrentPostingFromDt = MappingUtilities.ParseDates(CurrentPostingEffectiveFromDate),
            ParticipantId = long.TryParse(ParticipantId, out var partId) ? partId : 0
        };
        return cohortDis;
    }

    private Guid GetRequestId()
    {
        if (Guid.TryParse(RequestId, out var requestId))
        {
            return requestId;
        }
        return Guid.Empty;
    }
}
