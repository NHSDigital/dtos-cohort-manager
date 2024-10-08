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

        dateString = dateString.Split(' ')[0];
        DateTime tempDate = new DateTime();
        string[] formats = { "dd/MM/yyyy", "yyyyMMdd", "M/d/yyyy" };
        bool success = DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate);

        if (!success) _logger.LogError("Failed to parse date: {DateString}", dateString);

        return tempDate;
    }

    public object ParseDateTime(string dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString)) return DBNull.Value;
        if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, out DateTime datetime)) return datetime;

        return DBNull.Value;
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
