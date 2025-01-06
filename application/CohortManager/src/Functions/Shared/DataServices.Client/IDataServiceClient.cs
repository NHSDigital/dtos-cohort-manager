namespace DataServices.Client;

using System.Linq.Expressions;

public interface IDataServiceClient<TEntity>
{
    Task<IEnumerable<TEntity>> GetAll();
    Task<TEntity> GetSingle(string id);
    Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity,bool>> predicate);
    Task<bool> Add(TEntity entity);
    Task<bool> AddRange(IEnumerable<TEntity> entity);
    Task<bool> Update(TEntity entity);

}
