namespace DataServices.Core;

using System.Linq.Expressions;

public interface IDataServiceAccessor<TEntity>
{
    Task<TEntity> GetSingle(Expression<Func<TEntity, bool>> predicate);
    Task<List<TEntity>> GetRange(Expression<Func<TEntity, bool>> predicates);
    Task<bool> InsertSingle(TEntity entity);
    Task<bool> InsertMany(IEnumerable<TEntity> entities);
    Task<bool> Remove(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate);
    /// <summary>
    /// Upserts (Insert or Update) a single entity atomically using database MERGE
    /// </summary>
    /// <param name="entity">The entity to upsert</param>
    /// <param name="predicate">The predicate to match existing records</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> Upsert(TEntity entity, Expression<Func<TEntity, bool>> predicate);
}
