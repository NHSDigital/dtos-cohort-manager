namespace Common;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;

public static class EnumHelper
{
    /// <summary>
    /// Get the Display Attribute Name of an Enum
    /// </summary>
    public static string GetDisplayName(Enum enumValue)
    {
        var displayName = enumValue.GetType()
            .GetMember(enumValue.ToString())[0]
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName();

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = enumValue.ToString();
        }

        return displayName;
    }

    /// <summary>
    /// Gets list of all Http Status Codes
    /// </summary>
    /// <returns> List of all Http Status Code Numbers as string </returns>
    public static List<string> GetHttpStatusCodeStringList()
    {
        return ((HttpStatusCode[])Enum.GetValues(typeof(HttpStatusCode)))
                .Select(code => ((int)code).ToString())
                .ToList();
    }
}

