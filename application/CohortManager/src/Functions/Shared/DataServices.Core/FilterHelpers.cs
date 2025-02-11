namespace DataServices.Core;

using System.Linq.Expressions;
using Common;

internal static class FilterHelpers
{
    internal static Expression<Func<TEntity, bool>> CreateGetByKeyExpression<TEntity>(string filter,  string keyName) where TEntity : class
    {
        var entityParameter = Expression.Parameter(typeof(TEntity));
        var entityKey = Expression.Property(entityParameter, keyName);
        var filterConstant = Expression.Constant(Convert.ChangeType(filter, ReflectionUtilities.GetPropertyType(typeof(TEntity), keyName)));
        var expr = Expression.Equal(entityKey, filterConstant);

        return Expression.Lambda<Func<TEntity, bool>>(expr, entityParameter);
    }
}
