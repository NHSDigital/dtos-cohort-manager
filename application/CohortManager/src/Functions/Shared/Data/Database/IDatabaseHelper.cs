namespace Data.Database;

public interface IDatabaseHelper
{
    object ConvertNullToDbNull(string value);
    bool CheckIfNumberNull(string property);
}
