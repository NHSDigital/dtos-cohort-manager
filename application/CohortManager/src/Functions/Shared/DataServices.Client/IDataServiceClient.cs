namespace DataServices.Client;

using System.Linq.Expressions;

public interface IDataServiceClient<TEntity>
{
    Task<IEnumerable<TEntity>> GetAll();
    Task<TEntity> GetSingle(string id);
    Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity,bool>> predicate);

}