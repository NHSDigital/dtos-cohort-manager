namespace Common;

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

        string[] formats = ["dd/MM/yyyy", "yyyyMMdd", "M/d/yyyy", "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:ss.fffffff"];
        bool success = DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDate);

        if (!success)
            return null;

        return tempDate;
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
