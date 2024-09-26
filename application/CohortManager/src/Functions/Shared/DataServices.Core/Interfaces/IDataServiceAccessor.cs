public interface IDataServiceAccessor<TEntity>
{
    Task<TEntity> GetSingle(Func<TEntity,bool> predicate);
    Task<List<TEntity>> GetRange(Func<TEntity,bool> predicates);
    Task<bool> InsertSingle(TEntity entity);
}
