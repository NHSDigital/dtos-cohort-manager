namespace DataServices.Client;

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;

public class DataServiceStaticCachedClient<TEntity> : IDataServiceClient<TEntity> where TEntity : class
{
    private readonly ILogger<DataServiceStaticCachedClient<TEntity>> _logger;
    private readonly List<TEntity> _data;
    private readonly PropertyInfo _keyInfo;
    public DataServiceStaticCachedClient(
        ILogger<DataServiceStaticCachedClient<TEntity>> logger,
        DataServiceResolver dataServiceResolver,
        IHttpClientFunction httpClientFunction)
    {

        _logger = logger;
        var baseUrl = dataServiceResolver.GetDataServiceUrl(typeof(TEntity));

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidDataException($"Unable to resolve DataServiceUrl for Data Service of type: {typeof(TEntity).FullName}");
        }


        _logger = logger;

        _keyInfo = ReflectionUtilities.GetKey<TEntity>();
        _logger.LogInformation("Pre-Loading data from data service {EntityName}", typeof(TEntity).FullName);
        var jsonString = httpClientFunction.SendGet(baseUrl).Result;
        if (string.IsNullOrEmpty(jsonString))
        {
            throw new InvalidDataException($"No Data was available to be statically cached for the data Service Client of type: {typeof(TEntity).FullName}");
        }

        _data = JsonSerializer.Deserialize<List<TEntity>>(jsonString);
        if (_data == null)
        {
            throw new InvalidDataException($"No Data was available to be statically cached for the data Service Client of type: {typeof(TEntity).FullName}");
        }

        _logger.LogInformation("Pre-Loading data complete for data service {EntityName}", typeof(TEntity).FullName);

    }

    public async Task<TEntity> GetSingle(string id)
    {
        _logger.LogInformation("Getting Single from static data service {EntityName}", typeof(TEntity).FullName);
        await Task.CompletedTask;
        var predicate = CreateGetByKeyExpression(id).Compile();
        return _data.SingleOrDefault(predicate);
    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        _logger.LogInformation("Getting all Data from static data service {EntityName}", typeof(TEntity).FullName);
        await Task.CompletedTask;
        return _data.ToList();
    }

    public async Task<TEntity> GetSingleByFilter(Expression<Func<TEntity, bool>> predicate)
    {
        _logger.LogInformation("Getting Single By Filter from static data service {EntityName}", typeof(TEntity).FullName);
        await Task.CompletedTask;
        var predicateFunction = predicate.Compile();
        return _data.FirstOrDefault(predicateFunction);
    }

    public async Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity, bool>> predicate)
    {
        _logger.LogInformation("Getting By Filter from static data service {EntityName}", typeof(TEntity).FullName);
        await Task.CompletedTask;
        var predicateFunction = predicate.Compile();
        return _data.Where(predicateFunction).ToList();
    }

    public Task<bool> Add(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AddRange(IEnumerable<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Delete(string id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Update(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Upsert(TEntity entity)
    {
        throw new NotImplementedException();
    }

    private Expression<Func<TEntity, bool>> CreateGetByKeyExpression(string filter)
    {
        var entityParameter = Expression.Parameter(typeof(TEntity));
        var entityKey = Expression.Property(entityParameter, _keyInfo.Name);
        var filterConstant = Expression.Constant(Convert.ChangeType(filter, ReflectionUtilities.GetPropertyType(typeof(TEntity), _keyInfo.Name)));
        var expr = Expression.Equal(entityKey, filterConstant);

        return Expression.Lambda<Func<TEntity, bool>>(expr, entityParameter);
    }
}
