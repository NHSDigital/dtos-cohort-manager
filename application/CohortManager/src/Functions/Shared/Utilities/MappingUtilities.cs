namespace NHS.CohortManager.Shared.Utilities;

using System.Globalization;

public static class MappingUtilities
{
    public static DateTime? ParseNullableDateTime(string dateTimeString)
    {
        if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Parses flags that can be Y, N, 0 or 1, such as the exception flag
    /// </summary>
    public static short ParseStringFlag(string flag)
    {
        return flag.ToUpper() switch
        {
            "0" => 0,
            "1" => 1,
            "Y" => 1,
            "N" => 0,
            _ => throw new ArgumentException("Invalid input")
        };
    }

    /// <summary>
    /// Parses a date string to a nullable DateTime.
    /// Can handle partial dates.
    /// </summary>
    public static DateTime? ParseDates(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;

        if (dateString.Length == 4 || dateString.Length == 6)
        {
            dateString = HandlePartialDates(dateString);
        }

        string[] formats = ["dd/MM/yyyy", "yyyyMMdd", "M/d/yyyy", "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:ss.fffffff"];
        bool success = DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDate);

        if (!success)
            return null;

        return tempDate;
    }

    /// <summary>
    /// Returns a formatted date string given a nullable DateTime.
    /// Formatted according to the yyyy-MM-dd hh:mm:ss format.
    /// </summary>
    public static string? FormatDateTime(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }

    private static string HandlePartialDates(string dateString)
    {
        if (dateString.Length == 4)
        {
            return $"{dateString}0101";
        }

        if (dateString.Length == 6)
        {
            return $"{dateString}01";
        }

        return dateString;
    }
}
