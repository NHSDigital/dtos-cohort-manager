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
            Extracted = null
        };
    }
}
