namespace NHS.CohortManager.ScreeningValidationService;
public interface IDataLookupFacadeBreastScreening
{
    bool CheckIfPrimaryCareProviderExists(string primaryCareProvider);
    bool ValidateOutcode(string postcode);
    bool CheckIfCurrentPostingExists(string currentPosting);
    bool ValidatePostingCategories(string currentPosting);
    bool CheckIfPrimaryCareProviderInExcludedSmuList(string primaryCareProvider);
    string RetrievePostingCategory(string currentPosting);
}

