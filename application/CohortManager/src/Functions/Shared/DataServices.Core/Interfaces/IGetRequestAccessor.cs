namespace DataServices.Core;

public interface IGetRequestAccessor<TEntity> where TEntity: class
{
    Task<object> Get(FilterRequest<TEntity> request);
}
