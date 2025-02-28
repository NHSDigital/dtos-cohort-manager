namespace Data.Database;

using Model;
using Model.DTO;
using Model.Enums;

public static class CreateCohortDistributionParticipantDTO
{

    public static List<CohortDistributionParticipantDto> CohortDistributionParticipantDto(List<CohortDistribution> listOfAllParticipants)
    {
        return listOfAllParticipants.Select(s => new CohortDistributionParticipantDto
        {
            RequestId = s.RequestId.ToString(),
            NhsNumber = s.NHSNumber.ToString() ?? string.Empty,
            SupersededByNhsNumber = s.SupersededNHSNumber.ToString() ?? string.Empty,
            PrimaryCareProvider = s.PrimaryCareProvider ?? string.Empty,
            PrimaryCareProviderEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.PrimaryCareProviderDate.ToString()),
            NamePrefix = s.NamePrefix ?? string.Empty,
            FirstName = s.FamilyName ?? string.Empty,
            OtherGivenNames = s.OtherGivenName ?? string.Empty,
            FamilyName = s.FamilyName ?? string.Empty,
            PreviousFamilyName = s.PreviousFamilyName ?? string.Empty,
            DateOfBirth = DatabaseHelper.FormatDateAPI(s.DateOfBirth.ToString()),
            Gender = Enum.TryParse(s?.Gender.ToString(), out Gender gender) ? gender : Gender.NotKnown,
            AddressLine1 = s.AddressLine1 ?? string.Empty,
            AddressLine2 = s.AddressLine2 ?? string.Empty,
            AddressLine3 = s.AddressLine3 ?? string.Empty,
            AddressLine4 = s.AddressLine4 ?? string.Empty,
            AddressLine5 = s.AddressLine5 ?? string.Empty,
            Postcode = s.PostCode ?? string.Empty,
            UsualAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.UsualAddressFromDt?.ToString()),
            DateOfDeath = DatabaseHelper.FormatDateAPI(s.DateOfDeath.ToString()),
            TelephoneNumber = s.TelephoneNumberHome ?? string.Empty,
            TelephoneNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.TelephoneNumberHomeFromDt.ToString()),
            MobileNumber = s.TelephoneNumberMob ?? string.Empty,
            MobileNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.TelephoneNumberMobFromDt.ToString()) ?? string.Empty,
            EmailAddress = s.EmailAddressHome ?? string.Empty,
            EmailAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.EmailAddressHomeFromDt.ToString()) ?? string.Empty,
            PreferredLanguage = s.PreferredLanguage ?? string.Empty,
            IsInterpreterRequired = int.TryParse(s.InterpreterRequired.ToString(), out var isInterpreterRequired) ? isInterpreterRequired : 0,
            ReasonForRemoval = s.ReasonForRemoval ?? string.Empty,
            ReasonForRemovalEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.ReasonForRemovalDate.ToString()),
            ParticipantId = s.ParticipantId.ToString() ?? string.Empty,
            IsExtracted = s.IsExtracted.ToString() ?? string.Empty,
        }).ToList();
    }

}