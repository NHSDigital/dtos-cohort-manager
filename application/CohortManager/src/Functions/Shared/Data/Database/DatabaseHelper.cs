namespace Data.Database;

using System.Globalization;
using Microsoft.Extensions.Logging;

public class DatabaseHelper : IDatabaseHelper
{
    private readonly ILogger<DatabaseHelper> _logger;

    public DatabaseHelper(ILogger<DatabaseHelper> logger)
    {
        _logger = logger;
    }

    public bool CheckIfDateNull(string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            return true;
        }
        return !DateTime.TryParse(property, out _);
    }

    public bool CheckIfNumberNull(string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            return true;
        }

        return !long.TryParse(property, out _);
    }

    public DateTime ParseDates(string dateString)
    {
        dateString = dateString.Split(' ')[0];
        DateTime tempDate = new DateTime();
        string[] formats = { "dd/MM/yyyy", "yyyyMMdd", "M/d/yyyy" };
        bool success = DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDate);

        if (!success)
        {
            _logger.LogError($"****Failed to parse date: {dateString}");
        }

        return tempDate;
    }

    public object ConvertNullToDbNull(string value)
    {
        return string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    public string ParseDateToString(string dateToParse)
    {
        return (DateTime.ParseExact(dateToParse, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToString();
    }
}
