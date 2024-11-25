namespace Data.Database;

[Obsolete("Deprecated, use the LookupFacade instead",true)]
public interface IDbLookupValidationBreastScreening
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool ValidateLanguageCode(string languageCode);
    bool CheckIfCurrentPostingExists(string currentPosting);
    bool ValidatePostingCategories(string currentPosting);
    string RetrieveBSOCode(string postcode);
    string RetrievePostingCategory(string currentPosting);
    bool CheckIfPrimaryCareProviderInExcludedSmuList(string primaryCareProvider);
}
