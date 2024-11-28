namespace NHS.CohortManager.ScreeningValidationService;
public interface IDataLookupFacade
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool ValidateLanguageCode(string languageCode);
    bool CheckIfCurrentPostingExists(string currentPosting);
    bool ValidatePostingCategories(string currentPosting);
    bool CheckIfPrimaryCareProviderInExcludedSmuList(string primaryCareProvider);
    string RetrievePostingCategory(string currentPosting);
}

