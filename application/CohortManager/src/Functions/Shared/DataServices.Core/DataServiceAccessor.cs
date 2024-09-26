namespace DataServices.Core;

using DataServices.Database;
using Microsoft.EntityFrameworkCore;

public class DataServiceAccessor<TEntity> : IDataServiceAccessor<TEntity> where TEntity : class
{
    private readonly DataServicesContext _context;
    public DataServiceAccessor(DataServicesContext context)
    {
        _context = context;


    }

    public async Task<TEntity> GetSingle(Func<TEntity,bool> predicate)
    {
       var result = await _context.Set<TEntity>().FindAsync(predicate);
       return result;
    }

    public async Task<List<TEntity>> GetRange(Func<TEntity,bool> predicates)
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

    private bool Exists<TEntity>(TEntity entity)
    {
        return this.Set<TEntity>().Local.Any(e => e == entity);
    }


}

