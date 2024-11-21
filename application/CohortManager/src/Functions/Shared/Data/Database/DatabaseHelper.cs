namespace Data.Database;

using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Model.Enums;

public class DatabaseHelper : IDatabaseHelper
{
    private readonly ILogger<DatabaseHelper> _logger;

    public DatabaseHelper(ILogger<DatabaseHelper> logger)
    {
        _logger = logger;
    }

    public bool CheckIfNumberNull(string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            return true;
        }

        return !long.TryParse(property, out _);
    }

    public object ParseDates(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return DBNull.Value;

        string[] formats = ["dd/MM/yyyy", "yyyyMMdd", "M/d/yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss.fffZ"];
        bool success = DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDate);

        if (!success)
        {
            _logger.LogError("Failed to parse date: {DateString}", dateString);
            return DBNull.Value;
        }

        return tempDate;
    }

    public static string? FormatDateAPI(string date)
    {
        const string format = "yyyyMMdd";

        if (!DateTime.TryParse(date?.Trim(), out var parsedDate))
        {
            return string.Empty;
        }

        return parsedDate.ToString(format);
    }

    public object ConvertNullToDbNull(string value)
    {
        return string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    public static string GetStringValue(IDataReader reader, string columnName)
    {
        return reader[columnName] == DBNull.Value ? null : reader[columnName].ToString();
    }

    public static Gender GetGenderValue(IDataReader reader, string columnName)
    {
        return reader[columnName] == DBNull.Value ? Gender.NotKnown : (Gender)(short)reader[columnName];
    }

    public static T? GetValue<T>(IDataReader reader, string columnName)
    {
        object value = reader[columnName];

        if (value == DBNull.Value || value == null) return default;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public int ConvertBoolStringToInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;

        return value.Equals("true", StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
    }

    public int ParseExceptionFlag(object exception)
    {
        return exception != DBNull.Value && exception.ToString() == "Y" || exception == "1" ? 1 : 0;
    }
}
