namespace Common;

using Model;

public class CreateBasicParticipantData : ICreateBasicParticipantData
{
    public BasicParticipantData BasicParticipantData(Participant participant)
    {
        return new BasicParticipantData
        {
            RecordType = participant.RecordType,
            ChangeTimeStamp = participant.ChangeTimeStamp,
            SerialChangeNumber = participant.SerialChangeNumber,
            NhsNumber = participant.NhsNumber,
            SupersededByNhsNumber = participant.SupersededByNhsNumber,
            PrimaryCareProviderEffectiveFrom = participant.PrimaryCareProviderEffectiveFromDate,
            CurrentPostingEffectiveFrom = participant.CurrentPostingEffectiveFromDate,
            PreviousPosting = participant.PreviousPosting,
            PreviousPostingEffectiveFrom = participant.PreviousPostingEffectiveFromDate,
            OtherGivenNames = participant.OtherGivenNames,
            PreviousSurname = participant.PreviousSurname,
            AddressLine5 = participant.AddressLine5,
            PafKey = participant.PafKey,
            UsualAddressEffectiveFromDate = participant.UsualAddressEffectiveFromDate,
            TelephoneNumberEffectiveFromDate = participant.TelephoneNumberEffectiveFromDate,
            MobileNumber = participant.MobileNumber,
            MobileNumberEffectiveFromDate = participant.MobileNumberEffectiveFromDate,
            EmailAddressEffectiveFromDate = participant.EmailAddressEffectiveFromDate,
            InvalidFlag = participant.InvalidFlag,
            ChangeReasonCode = participant.ChangeReasonCode,
            RemovalReason = participant.ReasonForRemoval,
            RemovalEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate
        };
    }
}
