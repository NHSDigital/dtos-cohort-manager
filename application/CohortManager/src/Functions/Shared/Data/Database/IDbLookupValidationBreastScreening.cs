namespace Data.Database;


public interface IDbLookupValidationBreastScreening
{
    public bool ValidatePrimaryCareProvider(string primaryCareProvider);
    public bool ValidateOutcode(string postcode);
}