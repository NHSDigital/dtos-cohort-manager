namespace Data.Database;


public interface IDbLookupValidationBreastScreening
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool ValidateLanguageCode(string languageCode);
    bool ValidateCurrentPosting(string currentPosting, string primaryCareProvider);
}
