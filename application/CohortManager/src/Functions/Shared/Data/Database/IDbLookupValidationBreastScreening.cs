namespace Data.Database;


public interface IDbLookupValidationBreastScreening
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool ValidateLanguageCode(string languageCode);
    bool CheckIfCurrentPostingExists(string currentPosting);
    bool ValidatePostingCategories(string currentPosting);
    string GetBSOCode(string postcode);
}
