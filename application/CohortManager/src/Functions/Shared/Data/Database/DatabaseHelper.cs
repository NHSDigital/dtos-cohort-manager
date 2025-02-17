namespace Data.Database;

using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging;

public class DatabaseHelper : IDatabaseHelper
{

    public bool CheckIfNumberNull(string property)
    {
        if (string.IsNullOrEmpty(property))
        {
            return true;
        }

        return !long.TryParse(property, out _);
    }

    public static string? FormatDateAPI(string date)
    {
        const string format = "yyyyMMdd";

        if (!DateTime.TryParse(date?.Trim(), CultureInfo.InvariantCulture, out var parsedDate))
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
    public static T? GetValue<T>(IDataReader reader, string columnName)
    {
        object value = reader[columnName];
        if (value == DBNull.Value || value == null) return default;

        Type targetType = typeof(T);

        switch (targetType)
        {
            case Type t when t == typeof(string):
                if (value is DateTime time)
                {
                    return (T)(object)time.ToString();
                }
                if (value is Guid)
                {
                    return (T)(object)value.ToString();
                }
                return (T)(object)value.ToString();

            case Type t when t == typeof(Guid):
                {
                    return (T)value;
                }

            case Type t when t == typeof(DateTime):
                {
                    return (T)value;
                }
            case Type t when t.IsEnum:
                {
                    short shortValue = Convert.ToInt16(value);
                    return (T)Enum.ToObject(targetType, shortValue);
                }
            default:
                {
                    return (T)Convert.ChangeType(value, targetType);
                }
        }
    }

    public static object ConvertBoolStringToBoolByType(string environmentVariableName, string dataType)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);
        var isTrue = value?.Equals("true", StringComparison.CurrentCultureIgnoreCase) == true || value == "1";

        return dataType.ToLower() switch
        {
            "int" => isTrue ? 1 : 0,
            "bool" => isTrue,
            _ => throw new NotImplementedException()
        };
    }
}
