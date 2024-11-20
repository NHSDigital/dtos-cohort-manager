namespace NHS.CohortManager.CohortDistribution;
public interface ITransformDataLookupFacade
{
    bool ValidateOutcode(string postcode);
    string GetBsoCode(string postcode);
}
