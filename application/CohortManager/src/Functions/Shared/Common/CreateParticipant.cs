namespace Common;

using Model;

public class CreateParticipant : ICreateParticipant
{
    public Participant CreateResponseParticipantModel(BasicParticipantData participant, Demographic demographic)
    {
        return new Participant
        {
            RecordType = participant.RecordType,
            ChangeTimeStamp = participant.ChangeTimeStamp,
            SerialChangeNumber = participant.SerialChangeNumber,
            NhsNumber = participant.NhsNumber,
            SupersededByNhsNumber = participant.SupersededByNhsNumber,
            PrimaryCareProvider = demographic.PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = participant.PrimaryCareProviderEffectiveFrom,
            CurrentPosting = demographic.CurrentPosting,
            CurrentPostingEffectiveFromDate = participant.CurrentPostingEffectiveFrom,
            PreviousPosting = participant.PreviousPosting,
            PreviousPostingEffectiveFromDate = participant.PreviousPostingEffectiveFrom,
            NamePrefix = demographic.NamePrefix,
            FirstName = demographic.FirstName,
            OtherGivenNames = participant.OtherGivenNames,
            Surname = demographic.Surname,
            PreviousSurname = participant.PreviousSurname,
            DateOfBirth = demographic.DateOfBirth,
            Gender = demographic.Gender.GetValueOrDefault(),
            AddressLine1 = demographic.AddressLine1,
            AddressLine2 = demographic.AddressLine2,
            AddressLine3 = demographic.AddressLine3,
            AddressLine4 = demographic.AddressLine4,
            AddressLine5 = participant.AddressLine5,
            Postcode = demographic.Postcode,
            PafKey = participant.PafKey,
            UsualAddressEffectiveFromDate = participant.UsualAddressEffectiveFromDate,
            ReasonForRemoval = participant.RemovalReason,
            ReasonForRemovalEffectiveFromDate = participant.RemovalEffectiveFromDate,
            DateOfDeath = demographic.DateOfDeath,
            DeathStatus = demographic.DeathStatus,
            TelephoneNumber = demographic.TelephoneNumber,
            TelephoneNumberEffectiveFromDate = participant.TelephoneNumberEffectiveFromDate,
            MobileNumber = participant.MobileNumber,
            MobileNumberEffectiveFromDate = participant.MobileNumberEffectiveFromDate,
            EmailAddress = demographic.EmailAddress,
            EmailAddressEffectiveFromDate = participant.EmailAddressEffectiveFromDate,
            PreferredLanguage = demographic.PreferredLanguage,
            IsInterpreterRequired = demographic.IsInterpreterRequired,
            InvalidFlag = participant.InvalidFlag,
            ChangeReasonCode = participant.ChangeReasonCode
        };
    }

    public CohortDistributionParticipant CreateCohortDistributionParticipantModel(Participant participant, Demographic demographic)
    {
        return new CohortDistributionParticipant
        {
            ParticipantId = participant.ParticipantId,
            NhsNumber = participant.NhsNumber,
            SupersededByNhsNumber = participant.SupersededByNhsNumber,
            PrimaryCareProvider = demographic.PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = demographic.PrimaryCareProviderEffectiveFromDate,
            NamePrefix = demographic.NamePrefix,
            FirstName = demographic.FirstName,
            OtherGivenNames = participant.OtherGivenNames,
            Surname = demographic.Surname,
            PreviousSurname = participant.PreviousSurname,
            DateOfBirth = demographic.DateOfBirth,
            Gender = demographic.Gender.GetValueOrDefault(),
            AddressLine1 = demographic.AddressLine1,
            AddressLine2 = demographic.AddressLine2,
            AddressLine3 = demographic.AddressLine3,
            AddressLine4 = demographic.AddressLine4,
            AddressLine5 = participant.AddressLine5,
            Postcode = demographic.Postcode,
            UsualAddressEffectiveFromDate = participant.UsualAddressEffectiveFromDate,
            DateOfDeath = demographic.DateOfDeath,
            TelephoneNumber = demographic.TelephoneNumber,
            TelephoneNumberEffectiveFromDate = participant.TelephoneNumberEffectiveFromDate,
            MobileNumber = participant.MobileNumber,
            MobileNumberEffectiveFromDate = participant.MobileNumberEffectiveFromDate,
            EmailAddress = demographic.EmailAddress,
            EmailAddressEffectiveFromDate = participant.EmailAddressEffectiveFromDate,
            PreferredLanguage = demographic.PreferredLanguage,
            IsInterpreterRequired = demographic.IsInterpreterRequired,
            ReasonForRemoval = participant.ReasonForRemoval,
            ReasonForRemovalEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate,
            RecordInsertDateTime = demographic.RecordInsertDateTime,
            RecordUpdateDateTime = demographic.RecordUpdateDateTime,
            ScreeningAcronym = participant.ScreeningAcronym,
            Extracted = null
        };
    }

    public Participant ConvertCohortDistributionRecordToParticipant(CohortDistributionParticipant cohortParticipant)
    {
        return new Participant
        {
            ParticipantId = cohortParticipant.ParticipantId,
            NhsNumber = cohortParticipant.NhsNumber,
            SupersededByNhsNumber = cohortParticipant.SupersededByNhsNumber,
            PrimaryCareProvider = cohortParticipant.PrimaryCareProvider,
            PrimaryCareProviderEffectiveFromDate = cohortParticipant.PrimaryCareProviderEffectiveFromDate,
            NamePrefix = cohortParticipant.NamePrefix,
            FirstName = cohortParticipant.FirstName,
            OtherGivenNames = cohortParticipant.OtherGivenNames,
            Surname = cohortParticipant.Surname,
            PreviousSurname = cohortParticipant.PreviousSurname,
            DateOfBirth = cohortParticipant.DateOfBirth,
            Gender = cohortParticipant.Gender.GetValueOrDefault(),
            AddressLine1 = cohortParticipant.AddressLine1,
            AddressLine2 = cohortParticipant.AddressLine2,
            AddressLine3 = cohortParticipant.AddressLine3,
            AddressLine4 = cohortParticipant.AddressLine4,
            AddressLine5 = cohortParticipant.AddressLine5,
            Postcode = cohortParticipant.Postcode,
            UsualAddressEffectiveFromDate = cohortParticipant.UsualAddressEffectiveFromDate,
            DateOfDeath = cohortParticipant.DateOfDeath,
            TelephoneNumber = cohortParticipant.TelephoneNumber,
            TelephoneNumberEffectiveFromDate = cohortParticipant.TelephoneNumberEffectiveFromDate,
            MobileNumber = cohortParticipant.MobileNumber,
            MobileNumberEffectiveFromDate = cohortParticipant.MobileNumberEffectiveFromDate,
            EmailAddress = cohortParticipant.EmailAddress,
            EmailAddressEffectiveFromDate = cohortParticipant.EmailAddressEffectiveFromDate,
            PreferredLanguage = cohortParticipant.PreferredLanguage,
            IsInterpreterRequired = cohortParticipant.IsInterpreterRequired,
            ReasonForRemoval = cohortParticipant.ReasonForRemoval,
            ReasonForRemovalEffectiveFromDate = cohortParticipant.ReasonForRemovalEffectiveFromDate,
            RecordInsertDateTime = cohortParticipant.RecordInsertDateTime,
            RecordUpdateDateTime = cohortParticipant.RecordUpdateDateTime,
            ScreeningAcronym = cohortParticipant.ScreeningAcronym,
        };
    }
}
