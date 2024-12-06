namespace Common;
public static class MappingUtilities{
    public static DateTime? ParseNullableDateTime(string dateTimeString)
    {
        if(DateTime.TryParse(dateTimeString,out var result))
        {
            return result;
        }
        return null;
    }
}
