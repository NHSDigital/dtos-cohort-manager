using Common;
using DataServices.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class DataServiceCacheClient<TEntity> : DataServiceClient<TEntity> where TEntity : class
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DataServiceCacheClient<TEntity>> _logger;
    public DataServiceCacheClient(ILogger<DataServiceClient<TEntity>> logger, DataServiceResolver dataServiceResolver, ICallFunction callFunction, IMemoryCache memoryCache, ILogger<DataServiceCacheClient<TEntity>> cacheLogger) : base(logger, dataServiceResolver, callFunction)
    {
        _cache = memoryCache;
        _logger = cacheLogger;
    }

    public override async Task<TEntity> GetSingle(string id)
    {
        if(_cache.TryGetValue<TEntity>(id, out TEntity entity))
        {
            _logger.LogInformation("Cache Hit!");
            return entity;
        }
        _logger.LogInformation("Cache Miss :(");
        entity = await base.GetSingle(id);
        if(entity == null)
        {

            return null;
        }

        return _cache.Set<TEntity>(id,entity);
    }

}
