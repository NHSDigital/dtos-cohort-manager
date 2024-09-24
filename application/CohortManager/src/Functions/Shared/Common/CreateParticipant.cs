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
            CurrentPosting = demographic.CurrentPosting,
            NamePrefix = demographic.NamePrefix,
            FirstName = demographic.FirstName,
            OtherGivenNames = demographic.OtherGivenNames,
            Surname = demographic.Surname,
            PreviousSurname = demographic.PreviousSurname,
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
            ScreeningName = participant.ScreeningName
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
            Surname = demographic.Surname,
            PreviousSurname = demographic.PreviousSurname,
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
        };
    }
}
