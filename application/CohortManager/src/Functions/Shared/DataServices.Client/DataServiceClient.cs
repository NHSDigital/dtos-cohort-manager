namespace DataServices.Client;

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Net;

public class DataServiceClient<TEntity> : IDataServiceClient<TEntity> where TEntity : class
{
    private readonly string _baseUrl;
    private readonly ILogger<DataServiceClient<TEntity>> _logger;
    private readonly ICallFunction _callFunction;
    public DataServiceClient(ILogger<DataServiceClient<TEntity>> logger, DataServiceResolver dataServiceResolver, ICallFunction callFunction)
    {

        _baseUrl = dataServiceResolver.GetDataServiceUrl(typeof(TEntity));

        if (string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidDataException($"Unable to resolve DataServiceUrl {typeof(TEntity).Name}");
        }
        _callFunction = callFunction;
        _logger = logger;

    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        var jsonString = await _callFunction.SendGet(_baseUrl);
        IEnumerable<TEntity> result = JsonSerializer.Deserialize<IEnumerable<TEntity>>(jsonString);
        return result;
    }

    public async Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity, bool>> predicate)
    {

        try
        {
            //Resolves the constants
            var expr = new ClosureResolver().Visit(predicate);
            _logger.LogWarning(expr.ToString());


            var jsonString = await _callFunction.SendGet(_baseUrl, new Dictionary<string, string> { { "query", expr.ToString() } });
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }
            IEnumerable<TEntity> result = JsonSerializer.Deserialize<IEnumerable<TEntity>>(jsonString);
            return result;

        }
        catch (WebException wex)
        {
            HttpWebResponse response = (HttpWebResponse)wex.Response;
            if (response.StatusCode! == HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError(wex, "An Exception Happened while calling data service API");
            throw;
        }
    }

    public virtual async Task<TEntity> GetSingle(string id)
    {
        try
        {

            var jsonString = await _callFunction.SendGet(GetUrlBuilder(_baseUrl, id));

            if (string.IsNullOrEmpty(jsonString))
            {
                _logger.LogWarning("Response for get single from data service of type: {typeName} was empty", typeof(TEntity).FullName);
                return null;
            }

            TEntity result = JsonSerializer.Deserialize<TEntity>(jsonString);
            return result;
        }
        catch (WebException wex)
        {
            HttpWebResponse response = (HttpWebResponse)wex.Response;
            if (response.StatusCode! == HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError(wex, "An Exception Happened while calling data service API");
            throw;
        }
    }

    public async Task<bool> Delete(string id)
    {
        var result = await _callFunction.SendDelete(GetUrlBuilder(_baseUrl, id));
        return result;
    }
    public async Task<bool> AddRange(IEnumerable<TEntity> entity)
    {
        var jsonString = JsonSerializer.Serialize<IEnumerable<TEntity>>(entity);

        if(string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Unable to serialize post request body for creating entity of type {entityType}", typeof(TEntity).FullName);
            return false;
        }

        var result = await _callFunction.SendPost(_baseUrl,jsonString);

        if(result.StatusCode != HttpStatusCode.OK){
            return false;
        }
        return true;
    }

    public async Task<bool> Add(TEntity entity)
    {
        var jsonString = JsonSerializer.Serialize<TEntity>(entity);

        if(string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Unable to serialize post request body for creating entity of type {entityType}", typeof(TEntity).FullName);
            return false;
        }

        var result = await _callFunction.SendPost(_baseUrl,jsonString);

        if(result.StatusCode != HttpStatusCode.OK){
            return false;
        }
        return true;
    }

    private string GetUrlBuilder(string baseUrl, string argument)
    {
        baseUrl = baseUrl.TrimEnd('/');
        argument = argument.TrimStart('/');
        return string.Format("{0}/{1}", baseUrl, argument);
    }

}
