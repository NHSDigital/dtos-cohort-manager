namespace DataServices.Core;

using System.Data.Common;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DataServiceAccessor<TEntity> : IDataServiceAccessor<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly ILogger<DataServiceAccessor<TEntity>> _logger;
    public DataServiceAccessor(DataServicesContext context, ILogger<DataServiceAccessor<TEntity>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TEntity> GetSingle(Expression<Func<TEntity, bool>> predicate)
    {
        var result = _context.Set<TEntity>().SingleOrDefault(predicate);
        await Task.CompletedTask;
        return result;
    }

    public async Task<List<TEntity>> GetRange(Expression<Func<TEntity, bool>> predicates)
    {
        var result = _context.Set<TEntity>().Where(predicates).ToList();
        await Task.CompletedTask;
        return result;
    }

    public async Task<bool> InsertSingle(TEntity entity)
    {
        await _context.AddAsync(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> InsertMany(IEnumerable<TEntity> entities)
    {
        await _context.AddRangeAsync(entities);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> Remove(Expression<Func<TEntity, bool>> predicate)
    {
        var result = _context.Set<TEntity>().SingleOrDefault(predicate);
        await Task.CompletedTask;
        if (result == null)
        {
            return false;
        }
        _context.Set<TEntity>().Remove(result);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TEntity> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate)
    {

        var existingEntity = _context.Set<TEntity>().AsNoTracking().SingleOrDefault(predicate);
        await Task.CompletedTask;

        if (existingEntity == null)
        {
            return null;
        }
        using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Update(entity);
        var rowsEffected  = await _context.SaveChangesAsync();
        if(rowsEffected == 1)
        {
            await _context.Database.CommitTransactionAsync();
            return entity;
        }
        else if(rowsEffected > 1)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Multiple Records were updated by PUT request, Changes have been Rolledback");
            throw new Exception("Multiple Records were updated by PUT request, Changes have been Rolledback");
        }
        _logger.LogError("No records were updated despite a record being found");
        throw new Exception("Multiple Records were updated by PUT request, Changes have been Rolledback");

    }

}

