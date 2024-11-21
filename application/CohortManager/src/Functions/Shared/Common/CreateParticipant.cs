namespace Common;

using Model;

public class CreateParticipant : ICreateParticipant
{
    public Participant CreateResponseParticipantModel(BasicParticipantData participant, Demographic demographic)
    {
        return new Participant
        {
            RecordType = participant.RecordType,
            NhsNumber = participant.NhsNumber,
            SupersededByNhsNumber = demographic.SupersededByNhsNumber,
            PrimaryCareProvider = demographic.PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = demographic.PrimaryCareProviderEffectiveFromDate,
            CurrentPosting = demographic.CurrentPosting,
            CurrentPostingEffectiveFromDate = demographic.CurrentPostingEffectiveFromDate,
            NamePrefix = demographic.NamePrefix,
            FirstName = demographic.FirstName,
            OtherGivenNames = demographic.OtherGivenNames,
            FamilyName = demographic.FamilyName,
            PreviousFamilyName = demographic.PreviousFamilyName,
            DateOfBirth = demographic.DateOfBirth,
            Gender = demographic.Gender.GetValueOrDefault(),
            AddressLine1 = demographic.AddressLine1,
            AddressLine2 = demographic.AddressLine2,
            AddressLine3 = demographic.AddressLine3,
            AddressLine4 = demographic.AddressLine4,
            AddressLine5 = demographic.AddressLine5,
            Postcode = demographic.Postcode,
            PafKey = demographic.PafKey,
            UsualAddressEffectiveFromDate = demographic.UsualAddressEffectiveFromDate,
            ReasonForRemoval = participant.RemovalReason,
            ReasonForRemovalEffectiveFromDate = participant.RemovalEffectiveFromDate,
            DateOfDeath = demographic.DateOfDeath,
            DeathStatus = demographic.DeathStatus,
            TelephoneNumber = demographic.TelephoneNumber,
            TelephoneNumberEffectiveFromDate = demographic.TelephoneNumberEffectiveFromDate,
            MobileNumber = demographic.MobileNumber,
            MobileNumberEffectiveFromDate = demographic.MobileNumberEffectiveFromDate,
            EmailAddress = demographic.EmailAddress,
            EmailAddressEffectiveFromDate = demographic.EmailAddressEffectiveFromDate,
            PreferredLanguage = demographic.PreferredLanguage,
            IsInterpreterRequired = demographic.IsInterpreterRequired,
            InvalidFlag = demographic.InvalidFlag,
            ScreeningId = participant.ScreeningId,
            ScreeningName = participant.ScreeningName,
            RecordInsertDateTime = demographic.RecordInsertDateTime,
            //Accepting null for eligibility flag is a temporary behavior until eligibility flag is included in the test files.
            EligibilityFlag = participant.EligibilityFlag
        };
    }


    public CohortDistributionParticipant CreateCohortDistributionParticipantModel(Participant participant, Demographic demographic)
    {
        return new CohortDistributionParticipant
        {
            ParticipantId = participant.ParticipantId,
            NhsNumber = participant.NhsNumber,
            SupersededByNhsNumber = demographic.SupersededByNhsNumber,
            PrimaryCareProvider = demographic.PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = demographic.PrimaryCareProviderEffectiveFromDate,
            NamePrefix = demographic.NamePrefix,
            FirstName = demographic.FirstName,
            OtherGivenNames = demographic.OtherGivenNames,
            FamilyName = demographic.FamilyName,
            PreviousFamilyName = demographic.PreviousFamilyName,
            DateOfBirth = demographic.DateOfBirth,
            Gender = demographic.Gender.GetValueOrDefault(),
            AddressLine1 = demographic.AddressLine1,
            AddressLine2 = demographic.AddressLine2,
            AddressLine3 = demographic.AddressLine3,
            AddressLine4 = demographic.AddressLine4,
            AddressLine5 = demographic.AddressLine5,
            Postcode = demographic.Postcode,
            UsualAddressEffectiveFromDate = demographic.UsualAddressEffectiveFromDate,
            DateOfDeath = demographic.DateOfDeath,
            TelephoneNumber = demographic.TelephoneNumber,
            TelephoneNumberEffectiveFromDate = demographic.TelephoneNumberEffectiveFromDate,
            MobileNumber = demographic.MobileNumber,
            MobileNumberEffectiveFromDate = demographic.MobileNumberEffectiveFromDate,
            EmailAddress = demographic.EmailAddress,
            EmailAddressEffectiveFromDate = demographic.EmailAddressEffectiveFromDate,
            PreferredLanguage = demographic.PreferredLanguage,
            IsInterpreterRequired = demographic.IsInterpreterRequired,
            ReasonForRemoval = participant.ReasonForRemoval,
            ReasonForRemovalEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate,
            RecordInsertDateTime = demographic.RecordInsertDateTime,
            RecordUpdateDateTime = demographic.RecordUpdateDateTime,
            ScreeningAcronym = participant.ScreeningAcronym,
            ScreeningServiceId = participant.ScreeningId,
            ScreeningName = participant.ScreeningName,
            Extracted = null,
            RecordType = participant.RecordType,
            CurrentPosting = demographic.CurrentPosting,
            CurrentPostingEffectiveFromDate = demographic.CurrentPostingEffectiveFromDate
        };
    }
}
