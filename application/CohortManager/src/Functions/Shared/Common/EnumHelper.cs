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
}

