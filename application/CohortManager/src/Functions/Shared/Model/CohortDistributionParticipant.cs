namespace Model;

using System.Globalization;
using Enums;

public class CohortDistributionParticipant
{
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
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingEffectiveFromDate { get; set; }
    public string? ParticipantId { get; set; }
    public string RecordType { get; set; }
    public string? InvalidFlag { get; set; }


    public CohortDistribution ToCohortDistributionParticipant()
    {
        return new CohortDistribution
        {
            RequestId = GetRequestId(),
            NHSNumber = long.Parse(NhsNumber),
            SupersededNHSNumber = long.TryParse(SupersededByNhsNumber, out var supNhsNum) ? supNhsNum : 0,
            PrimaryCareProvider = PrimaryCareProvider ?? string.Empty,
            PrimaryCareProviderDate = DateTime.TryParseExact(PrimaryCareProviderEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pcpDate) ? pcpDate : null,
            NamePrefix = NamePrefix,
            GivenName = FirstName,
            OtherGivenName = OtherGivenNames,
            FamilyName = FamilyName,
            PreviousFamilyName = PreviousFamilyName,
            DateOfBirth = DateTime.TryParse(DateOfBirth, out var dob) ? dob : null,
            Gender = (short?)Gender ?? 0,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            AddressLine3 = AddressLine3,
            AddressLine4 = AddressLine4,
            AddressLine5 = AddressLine5,
            PostCode = Postcode,
            UsualAddressFromDt = DateTime.TryParseExact(UsualAddressEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var uaDate) ? uaDate : null,
            DateOfDeath = DateTime.TryParse(DateOfDeath, null, out var dod) ? dod : null,
            TelephoneNumberHome = TelephoneNumber,
            TelephoneNumberHomeFromDt = DateTime.TryParseExact(TelephoneNumberEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var telDate) ? telDate : null,
            TelephoneNumberMob = MobileNumber,
            TelephoneNumberMobFromDt = DateTime.TryParseExact(MobileNumberEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var mobDate) ? mobDate : null,
            EmailAddressHome = EmailAddress,
            EmailAddressHomeFromDt = DateTime.TryParseExact(EmailAddressEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var emailDate) ? emailDate : null,
            PreferredLanguage = PreferredLanguage,
            InterpreterRequired = short.TryParse(IsInterpreterRequired, out var interpreter) ? interpreter : (short)0,
            ReasonForRemoval = ReasonForRemoval,
            ReasonForRemovalDate = DateTime.TryParseExact(ReasonForRemovalEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var remDate) ? remDate : null,
            IsExtracted = short.TryParse(Extracted, out var extracted) ? extracted : (short)0,
            RecordInsertDateTime = DateTime.TryParseExact(RecordInsertDateTime, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ridt) ? ridt : null,
            RecordUpdateDateTime = DateTime.TryParseExact(RecordUpdateDateTime, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rudt) ? rudt : null,
            CurrentPosting = CurrentPosting,
            CurrentPostingFromDt = DateTime.TryParseExact(CurrentPostingEffectiveFromDate, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out var cpd) ? cpd : null,
            ParticipantId = long.TryParse(ParticipantId, out var partId) ? partId : 0
        };
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
