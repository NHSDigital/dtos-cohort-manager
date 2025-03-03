namespace Common;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

public static class EnumHelper
{
    /// <summary>
    /// Get the Display Attribute Name of an Enum
    /// </summary>
    public static string GetDisplayName(Enum enumValue)
    {
        var displayName = "";
        try 
        {
            displayName = enumValue.GetType()
            .GetMember(enumValue.ToString())[0]
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName();
        } 
        catch (Exception) 
        {
            // There is nothing to do when catching the exception as expected output is to return empty string.
        }
        
        return displayName;
    }
}

