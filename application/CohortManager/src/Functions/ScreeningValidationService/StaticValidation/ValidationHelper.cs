namespace NHS.CohortManager.ScreeningValidationService;

using System.Globalization;

public static class ValidationHelper
{
    // Validates that the date is not in the future and that it is in one of the expected formats
    public static bool ValidatePastDate(string dateString)
    {
        DateTime date;

        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateString, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }

        return false;
    }
}
