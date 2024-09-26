namespace DataServices.Core;

using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DataServiceAccessor<TEntity> : IDataServiceAccessor<TEntity> where TEntity : class
{
    private readonly DataServicesContext _context;
    private readonly ILogger<DataServiceAccessor<TEntity>> _logger;
    public DataServiceAccessor(DataServicesContext context, ILogger<DataServiceAccessor<TEntity>> logger)
    {
        _context = context;
        _logger = logger;

        // var prrop = _context.GetType().GetProperty(nameof(TEntity));
        // _logger.LogError($"Logger error { prrop.Name }");


        var properties = context.GetType().GetProperties();

        foreach (var property in properties)
        {
            var setType = property.PropertyType;
            var isDbSet = setType.IsGenericType && (typeof (DbSet<>).IsAssignableFrom(setType.GetGenericTypeDefinition()));

            _logger.LogCritical($"{setType}");
        }

    }

    public async Task<TEntity> GetSingle(Func<TEntity,bool> predicate)
    {
       var result = _context.Set<TEntity>().SingleOrDefault(predicate);
       await Task.CompletedTask;
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

    public async Task<bool> Remove(Func<TEntity,bool> predicate)
    {
        var result = _context.Set<TEntity>().SingleOrDefault(predicate);
        await Task.CompletedTask;
        if(result == null)
        {
            return false;
        }
        _context.Set<TEntity>().Remove(result);
        await _context.SaveChangesAsync();
        return true;

    }


}

