namespace Common;

using Model;
using Model.Enums;

public class CreateParticipant : ICreateParticipant
{
    public Participant CreateResponseParticipantModel(BasicParticipantData participant, Demographic demographic)
    {
        return new Participant
        {
            RecordType = participant.RecordType,
            ChangeTimeStamp = participant.ChangeTimeStamp,
            SerialChangeNumber = participant.SerialChangeNumber,
            NHSId = participant.NHSId,
            SupersededByNhsNumber = participant.SupersededByNhsNumber,
            PrimaryCareProvider = demographic.GeneralPractitionerCode,
            PrimaryCareProviderEffectiveFromDate = participant.PrimaryCareProviderEffectiveFrom,
            CurrentPosting = demographic.ManagingOrganizationCode,
            CurrentPostingEffectiveFromDate = participant.CurrentPostingEffectiveFrom,
            PreviousPosting = participant.PreviousPosting,
            PreviousPostingEffectiveFromDate = participant.PreviousPostingEffectiveFrom,
            NamePrefix = demographic.Prefix,
            FirstName = demographic.GivenName,
            OtherGivenNames = participant.OtherGivenNames,
            Surname = demographic.FamilyName,
            PreviousSurname = participant.PreviousSurname,
            DateOfBirth = demographic.BirthDate,
            Gender = (Gender)Enum.Parse(typeof(Gender), demographic.Gender, true),
            AddressLine1 = demographic.HomeAddressLine1,
            AddressLine2 = demographic.HomeAddressLine2,
            AddressLine3 = demographic.HomeAddressLine3,
            AddressLine4 = demographic.HomeAddressCity,
            AddressLine5 = participant.AddressLine5,
            Postcode = demographic.HomeAddressPostcode,
            PafKey = participant.PafKey,
            UsualAddressEffectiveFromDate = participant.UsualAddressEffectiveFromDate,
            ReasonForRemoval = demographic.RemovalReasonCode,
            ReasonForRemovalEffectiveFromDate = demographic.RemovalEffectiveStart,
            DateOfDeath = demographic.DeceasedDatetime,
            DeathStatus = demographic.DeceasedDatetime != null ? Status.Formal : Status.Informal,
            TelephoneNumber = demographic.HomePhoneNumber,
            TelephoneNumberEffectiveFromDate = participant.TelephoneNumberEffectiveFromDate,
            MobileNumber = participant.MobileNumber,
            MobileNumberEffectiveFromDate = participant.MobileNumberEffectiveFromDate,
            EmailAddress = demographic.HomeEmailAddress,
            EmailAddressEffectiveFromDate = participant.EmailAddressEffectiveFromDate,
            PreferredLanguage = demographic.CommunicationLanguage,
            IsInterpreterRequired = demographic.InterpreterRequired,
            InvalidFlag = participant.InvalidFlag,
            RecordIdentifier = demographic.ResourceId,
            ChangeReasonCode = participant.ChangeReasonCode
        };
    }
}
