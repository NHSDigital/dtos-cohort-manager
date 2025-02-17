namespace DataServices.Client;

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;


public class DataServiceClient<TEntity> : IDataServiceClient<TEntity> where TEntity : class
{
    private readonly string _baseUrl;
    private readonly ILogger<DataServiceClient<TEntity>> _logger;
    private readonly ICallFunction _callFunction;
    private readonly PropertyInfo _keyInfo;
    public DataServiceClient(ILogger<DataServiceClient<TEntity>> logger, DataServiceResolver dataServiceResolver, ICallFunction callFunction)
    {

        _baseUrl = dataServiceResolver.GetDataServiceUrl(typeof(TEntity));

        if (string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidDataException($"Unable to resolve DataServiceUrl for Data Service of type: {typeof(TEntity).FullName}");
        }
        _callFunction = callFunction;
        _logger = logger;

        _keyInfo = ReflectionUtilities.GetKey<TEntity>();

    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        var jsonString = await _callFunction.SendGet(_baseUrl);
        if (string.IsNullOrEmpty(jsonString)) return [];

        return JsonSerializer.Deserialize<IEnumerable<TEntity>>(jsonString);
    }

    public async Task<IEnumerable<TEntity>> GetByFilter(Expression<Func<TEntity, bool>> predicate)
    {
        var jsonString = await GetJsonStringByFilter(predicate);
        if (string.IsNullOrEmpty(jsonString))
        {
            return [];
        }
        IEnumerable<TEntity> result = JsonSerializer.Deserialize<IEnumerable<TEntity>>(jsonString);
        return result;
    }

    public virtual async Task<TEntity> GetSingle(string id)
    {

        var httpResponse = await _callFunction.SendGetWebRequest(UrlBuilder(_baseUrl, id));
        var jsonString = await _callFunction.GetResponseText(httpResponse);
        if(httpResponse.StatusCode == HttpStatusCode.NotFound && jsonString == "No Data Found")
        {
            return null;
        }
        if(httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != HttpStatusCode.NoContent)
        {
            throw new WebException($"Call to {typeof(TEntity)} dataservice returned an bad response {httpResponse.StatusCode} {httpResponse.StatusDescription}");
        }

        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Response for get single from data service of type: {TypeName} was empty", typeof(TEntity).FullName);
            return null;
        }
        if (jsonString == "No Data Found")
        {
            return null;
        }

        TEntity result = JsonSerializer.Deserialize<TEntity>(jsonString);
        return result;

    }
    public async Task<TEntity> GetSingleByFilter(Expression<Func<TEntity, bool>> predicate)
    {

        var jsonString = await GetJsonStringByFilter(predicate, true);
        if (string.IsNullOrEmpty(jsonString))
        {
            return null;
        }
        TEntity result = JsonSerializer.Deserialize<TEntity>(jsonString);
        return result;

    }

    public async Task<bool> Delete(string id)
    {
        var result = await _callFunction.SendDelete(UrlBuilder(_baseUrl, id));
        return result;
    }
    public async Task<bool> AddRange(IEnumerable<TEntity> entities)
    {
        var jsonString = JsonSerializer.Serialize<IEnumerable<TEntity>>(entities);

        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Unable to serialize post request body for creating entity of type {EntityType}", typeof(TEntity).FullName);
            return false;
        }

        var result = await _callFunction.SendPost(_baseUrl, jsonString);

        if (result.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }
        return true;
    }

    public async Task<bool> Add(TEntity entity)
    {
        var jsonString = JsonSerializer.Serialize<TEntity>(entity);

        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Unable to serialize post request body for creating entity of type {EntityType}", typeof(TEntity).FullName);
            return false;
        }

        var result = await _callFunction.SendPost(_baseUrl, jsonString);

        if (result.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }
        return true;
    }

    public async Task<bool> Update(TEntity entity)
    {
        var jsonString = JsonSerializer.Serialize<TEntity>(entity);
        var key = _keyInfo.GetValue(entity).ToString();

        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogWarning("Unable to serialize put request body for creating entity of type {EntityType}", typeof(TEntity).FullName);
            return false;
        }

        var result = await _callFunction.SendPut(UrlBuilder(_baseUrl, key), jsonString);

        if (result.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }
        return true;
    }

    private async Task<string> GetJsonStringByFilter(Expression<Func<TEntity, bool>> predicate, bool returnOneRecord = false)
    {
        //Resolves the constants
        var expr = new ClosureResolver().Visit(predicate);

        var queryItems = new Dictionary<string, string> { { "query", expr.ToString() } };

        if (returnOneRecord)
        {
            queryItems.Add("single", "true");
        }

        var httpResponse = await _callFunction.SendGetWebRequest(_baseUrl, queryItems);

        if(httpResponse.StatusCode == HttpStatusCode.NoContent){
            return "[]";
        }
        if(httpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new WebException($"Call to {typeof(TEntity)} dataservice returned an bad response {httpResponse.StatusCode} {httpResponse.StatusDescription}");
        }

        var jsonString = await _callFunction.GetResponseText(httpResponse);


        return jsonString;


    }

    private static string UrlBuilder(string baseUrl, string argument)
    {
        baseUrl = baseUrl.TrimEnd('/');
        argument = argument.TrimStart('/');
        return string.Format("{0}/{1}", baseUrl, argument);
    }


}
