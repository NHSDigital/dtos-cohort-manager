namespace NHS.CohortManager.ScreeningValidationService;

using System.Globalization;

public static class Validators
{
    // Validates that the date is not in the future and that it is in one of the expected formats
    public static bool ValidateDateOfBirth(string dateOfBirth)
    {
        DateTime date;

        if (DateTime.TryParseExact(dateOfBirth, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateOfBirth, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateOfBirth, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }

        return false;
    }
}
