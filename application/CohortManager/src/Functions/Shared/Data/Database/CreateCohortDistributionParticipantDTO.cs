namespace Data.Database;

using Model;
using Model.DTO;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

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
            PrimaryCareProviderEffectiveFromDate = MappingUtilities.FormatDateTime(s.PrimaryCareProviderDate),
            NamePrefix = s.NamePrefix ?? string.Empty,
            FirstName = s.FamilyName ?? string.Empty,
            OtherGivenNames = s.OtherGivenName ?? string.Empty,
            FamilyName = s.FamilyName ?? string.Empty,
            PreviousFamilyName = s.PreviousFamilyName ?? string.Empty,
            DateOfBirth = MappingUtilities.FormatDateTime(s.DateOfBirth),
            Gender = Enum.TryParse(s?.Gender.ToString(), out Gender gender) ? gender : Gender.NotKnown,
            AddressLine1 = s.AddressLine1 ?? string.Empty,
            AddressLine2 = s.AddressLine2 ?? string.Empty,
            AddressLine3 = s.AddressLine3 ?? string.Empty,
            AddressLine4 = s.AddressLine4 ?? string.Empty,
            AddressLine5 = s.AddressLine5 ?? string.Empty,
            Postcode = s.PostCode ?? string.Empty,
            UsualAddressEffectiveFromDate = MappingUtilities.FormatDateTime(s.UsualAddressFromDt),
            DateOfDeath = MappingUtilities.FormatDateTime(s.DateOfDeath),
            TelephoneNumber = s.TelephoneNumberHome ?? string.Empty,
            TelephoneNumberEffectiveFromDate = MappingUtilities.FormatDateTime(s.TelephoneNumberHomeFromDt),
            MobileNumber = s.TelephoneNumberMob ?? string.Empty,
            MobileNumberEffectiveFromDate = MappingUtilities.FormatDateTime(s.TelephoneNumberMobFromDt) ?? string.Empty,
            EmailAddress = s.EmailAddressHome ?? string.Empty,
            EmailAddressEffectiveFromDate = MappingUtilities.FormatDateTime(s.EmailAddressHomeFromDt) ?? string.Empty,
            PreferredLanguage = s.PreferredLanguage ?? string.Empty,
            IsInterpreterRequired = int.TryParse(s.InterpreterRequired.ToString(), out var isInterpreterRequired) ? isInterpreterRequired : 0,
            ReasonForRemoval = s.ReasonForRemoval ?? string.Empty,
            ReasonForRemovalEffectiveFromDate = MappingUtilities.FormatDateTime(s.ReasonForRemovalDate),
            ParticipantId = s.ParticipantId.ToString() ?? string.Empty,
            IsExtracted = s.IsExtracted.ToString() ?? string.Empty,
        }).ToList();
    }

}