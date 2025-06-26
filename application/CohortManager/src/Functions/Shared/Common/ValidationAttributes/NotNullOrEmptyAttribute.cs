namespace Common;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Validates that a string property is not null or empty.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NotNullOrEmptyAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return true;
        }
        return false;
    }
}
