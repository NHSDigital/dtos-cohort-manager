namespace DataServices.Client;

using System.Linq.Expressions;

public interface IDataServiceClient<TEntity>
{
    /// <summary>
    /// Gets all items
    /// </summary>
    /// <returns>Returns a task</returns>
    Task<IEnumerable<TEntity>> GetAll();
    Task<TEntity> GetSingle(string id);
    Task<TEntity> GetSingleByFilter(Expression<Func<TEntity,bool>> predicate);
    Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity,bool>> predicate);
    Task<bool> Add(TEntity entity);
    Task<bool> AddRange(IEnumerable<TEntity> entity);

}
