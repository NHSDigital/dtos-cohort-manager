namespace Data.Database;

public interface IDatabaseHelper
{
    public DateTime ParseDates(string dateString);
    public object ConvertNullToDbNull(string value);
    public bool CheckIfDateNull(string property);
    public bool CheckIfNumberNull(string property);
    public string ParseDateToString(string dateToParse);
    public int ConvertBoolStringToInt(string value);
}
