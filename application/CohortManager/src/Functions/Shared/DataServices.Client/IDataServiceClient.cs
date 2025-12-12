namespace DataServices.Client;

using System.Linq.Expressions;

public interface IDataServiceClient<TEntity>
{
    /// <summary>
    /// Gets all items
    /// </summary>
    /// <returns>Returns a task with a result type of TEntity</returns>
    Task<IEnumerable<TEntity>> GetAll();
    /// <summary>
    /// Gets a single item given the primary key of the table given as an argument
    /// </summary>
    /// <param name="id">Primary key of table being queried</param>
    /// <returns>Returns a task with a result type of TEntity</returns>
    Task<TEntity> GetSingle(string id);
    /// <summary>
    /// Gets a single by an expression such as i => i.item == "This item"
    /// </summary>
    /// <param name="predicate">linq query defining the filter on the table</param>
    /// <returns>Returns a task with the result type of TEntity</returns>
    Task<TEntity> GetSingleByFilter(Expression<Func<TEntity, bool>> predicate);
    /// <summary>
    /// Get a list of items where they meet the given predicate
    /// </summary>
    /// <param name="predicate">linq query defining the filter on the table</param>
    /// <returns>Returns a task with the result type of IEnumerable<TEntity></returns>
    Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity, bool>> predicate);
    /// <summary>
    /// Adds a given records to the database
    /// </summary>
    /// <param name="entity">object of type TEntity to be inserted in the database</param>
    /// <returns>a boolean representing if the record was inserted successfully</returns>
    Task<bool> Add(TEntity entity);
    /// <summary>
    /// Adds an IEnumerable of type TEntity to the database
    /// </summary>
    /// <param name="entities">IEnumerable<TEntity> of items to be added to the database</param>
    /// <returns>a boolean representing if the record was inserted successfully</returns>
    Task<bool> AddRange(IEnumerable<TEntity> entities);
    /// <summary>
    /// Deletes a single record
    /// </summary>
    /// <param name="id">the id of the record to be deleted</param>
    /// <returns>a boolean representing if the record was deleted successfully</returns>
    Task<bool> Delete(string id);
    /// <summary>
    /// Updates a single Record
    /// </summary>
    /// <param name="entity">the object that is being updated/param>
    /// <returns>a boolean representing if the record was updated successfully</returns>
    Task<bool> Update(TEntity entity);
    /// <summary>
    /// Upserts (Inserts or Updates) a single record atomically
    /// </summary>
    /// <param name="entity">the object to be upserted</param>
    /// <returns>a boolean representing if the record was upserted successfully</returns>
    Task<bool> Upsert(TEntity entity);
}
