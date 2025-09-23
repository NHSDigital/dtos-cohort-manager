namespace DataServices.Client;

using Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class DataServiceCacheClient<TEntity> : DataServiceClient<TEntity> where TEntity : class
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DataServiceCacheClient<TEntity>> _logger;

    public DataServiceCacheClient(ILogger<DataServiceCacheClient<TEntity>> logger, DataServiceResolver dataServiceResolver, IHttpClientFunction httpClientFunction, IMemoryCache memoryCache, ILogger<DataServiceCacheClient<TEntity>> cacheLogger) : base(logger, dataServiceResolver, httpClientFunction)
    {
        _cache = memoryCache;
        _logger = cacheLogger;
    }

    public override async Task<TEntity> GetSingle(string id)
    {
        if (_cache.TryGetValue<TEntity>(id, out TEntity entity))
        {
            _logger.LogInformation("Cache Hit reading key {Key} for entity : {Entity}", id, typeof(TEntity).FullName);
            return entity;
        }
        _logger.LogInformation("Cache Miss reading key {Key} for entity : {Entity}", id, typeof(TEntity).FullName);
        entity = await base.GetSingle(id);
        if (entity == null)
        {
            return null;
        }
        return _cache.Set<TEntity>(id, entity, TimeSpan.FromHours(1));
    }

}
