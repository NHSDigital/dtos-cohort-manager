namespace DataServices.Core;

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
        var result = await _context.AddAsync(entity);
        await _context.SaveChangesAsync();
        return true;
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

    public async Task<bool> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate)
    {
        var existingEntity = _context.Set<TEntity>().SingleOrDefault(predicate);
        await Task.CompletedTask;

        if (existingEntity == null)
        {
            await _context.AddAsync(entity);
        }

        _context.Entry(existingEntity).CurrentValues.SetValues(entity);

        return await _context.SaveChangesAsync() > 0;
    }

}

