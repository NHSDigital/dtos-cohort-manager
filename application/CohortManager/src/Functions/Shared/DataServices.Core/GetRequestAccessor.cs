namespace DataServices.Core;

using Common;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetRequestAccessor<TEntity> : IGetRequestAccessor<TEntity>  where TEntity : class
{
    private readonly DbContext _context;
    private readonly ILogger<GetRequestAccessor<TEntity>> _logger;
    public GetRequestAccessor(DataServicesContext dbContext, ILogger<GetRequestAccessor<TEntity>> logger)
    {
        _context = dbContext;
        _logger = logger;
    }

    public async Task<object> Get(FilterRequest<TEntity> request)
    {
        var query = _context.Set<TEntity>().AsQueryable();

        foreach(var include in request.Includes)
        {
            if(!ReflectionUtilities.PropertyExists(typeof(TEntity),include))
            {
                throw new InvalidPropertyException($"Property by the name of {include} could not be included in the type of {typeof(TEntity).FullName}");
            }
            query = query.Include(include);
        }
        if(request.Where != null)
        {
            query = query.Where(request.Where);
        }
        if(request.OrderBy != null)
        {
            
            _logger.LogError("Orderby Is not Implemented");
        }
        if(request.Limit != 0)
        {
            query = query.Take(request.Limit);
        }
        if(request.Single)
        {
            var result = query.AsNoTracking().SingleOrDefaultAsync();
            return result;
        }
        else
        {
            var result = await query.AsNoTracking().ToListAsync();
            return result;
        }
    }


}
