namespace NHS.CohortManager.CohortDistributionService;

using Model;

public interface ITransformDataLookupFacade
{
    bool ValidateOutcode(string postcode);
    string GetBsoCode(string postcode);
    public string GetBsoCodeUsingPCP(string primaryCareProvider);
    public bool ValidateLanguageCode(string languageCode);
    public Task<HashSet<string>> GetCachedExcludedSMUValues();
}
