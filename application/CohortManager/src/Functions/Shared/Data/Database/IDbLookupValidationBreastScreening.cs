namespace Data.Database;


public interface IDbLookupValidationBreastScreening
{
    bool PrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool ValidateLanguageCode(string languageCode);
    bool ValidateCurrentPosting(string currentPosting, string primaryCareProvider);
}
