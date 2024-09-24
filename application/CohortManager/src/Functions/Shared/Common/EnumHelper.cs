namespace Common;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

public static class EnumHelper
{
    /// <summary>
    /// Get the Display name of an Enum
    /// </summary>
    public static string GetDisplayName(Enum enumValue)
    {
        var displayName = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName();

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = enumValue.ToString();
        }

        return displayName;
    }
}

