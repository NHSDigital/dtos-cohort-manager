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
}
