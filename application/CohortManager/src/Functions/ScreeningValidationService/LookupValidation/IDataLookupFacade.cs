public interface IDataLookupFacade
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    public bool ValidateOutcode(string postcode);
    public bool ValidateLanguageCode(string languageCode);
    bool CheckIfCurrentPostingExists(string currentPosting);
    bool ValidatePostingCategories(string currentPosting);
}
