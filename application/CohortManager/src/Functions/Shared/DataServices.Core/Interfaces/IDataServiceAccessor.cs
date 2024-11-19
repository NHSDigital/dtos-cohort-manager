namespace DataServices.Core;

using System.Linq.Expressions;

public interface IDataServiceAccessor<TEntity>
{
    Task<TEntity> GetSingle(Expression<Func<TEntity, bool>> predicate);
    Task<List<TEntity>> GetRange(Expression<Func<TEntity, bool>> predicates);
    Task<bool> InsertSingle(TEntity entity);
    Task<bool> Remove(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate);
}
