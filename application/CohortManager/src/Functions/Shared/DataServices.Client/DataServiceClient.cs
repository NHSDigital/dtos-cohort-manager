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

        if( string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidDataException("Unable to resolve DataServiceUrl");
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

    public async Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity,bool>> predicate)
    {
        //Resolves the constants
        var expr  = new ClosureResolver().Visit(predicate);
        _logger.LogWarning(expr.ToString());


        var jsonString = await _callFunction.SendGet(_baseUrl,new Dictionary<string, string>{{"query",expr.ToString()}});
        IEnumerable<TEntity> result = JsonSerializer.Deserialize<IEnumerable<TEntity>>(jsonString);
        return result;
    }

    public virtual async Task<TEntity> GetSingle(string id)
    {
        try{
            var jsonString = await _callFunction.SendGet(_baseUrl+id);
            TEntity result = JsonSerializer.Deserialize<TEntity>(jsonString);
            return result;
        }
        catch(WebException wex)
        {
            HttpWebResponse response = (HttpWebResponse)wex.Response;
            if(response.StatusCode! == HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError(wex,"An Exception Happened while calling data service API");
            throw;
        }
    }

    public async Task<bool> Delete(string id)
    {
        var result = await _callFunction.SendDelete(_baseUrl+id);
        return result;
    }

}
