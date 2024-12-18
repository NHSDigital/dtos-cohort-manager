using System.ComponentModel.DataAnnotations;
using System.Reflection;

public static class ReflectionUtilities
{
    public static PropertyInfo GetKey<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        return type.GetProperties().FirstOrDefault(p =>
            p.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));
    }
}
