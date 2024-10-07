namespace Data.Database;

public interface IDatabaseHelper
{
    object ParseDates(string dateString);
    object ConvertNullToDbNull(string value);
    bool CheckIfNumberNull(string property);
    int ConvertBoolStringToInt(string value);
    int ParseExceptionFlag(object exception);
    object ParseDateTime(string dateTimeString);
}
