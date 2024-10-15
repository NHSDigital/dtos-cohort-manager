namespace Common;

using System.Globalization;
using Model;

public static class DateTimeHelper
{

/// <summary>
/// Validates if the input string is in ISO 8601 date format.
/// </summary>
/// <param name="value">The date string to validate.</param>
/// <returns>
/// A tuple where the first item is a boolean indicating validity,
/// and the second item is the parsed DateTime or the
/// default value if parsing fails.
/// </returns>
    public static (bool isValidDateFormat, DateTime? date) IsValidDateFormat (string value)
    {
        bool isValidDateFormat = DateTime.TryParseExact(value, DateFormats.Iso8601, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);

        return (isValidDateFormat, date);
    }
}
