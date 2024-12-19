using System.ComponentModel.DataAnnotations;
using System.Reflection;

public static class ReflectionUtilities
{
    /// <summary>
    /// Gets the PropertyInfo of the Key for the Type Of TEntity Using Reflection
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns>Returns the Property Info of the element of the TEntity Class tagged with the [Key] Data Attribute</returns>
    public static PropertyInfo GetKey<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        return type.GetProperties().FirstOrDefault(p =>
            p.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));
    }
}
