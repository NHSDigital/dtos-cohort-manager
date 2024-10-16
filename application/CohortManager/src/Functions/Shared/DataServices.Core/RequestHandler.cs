
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.IdentityModel.Tokens;

public class RequestHandler<TEntity> : IRequestHandler<TEntity> where TEntity : class
{

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    private ILogger<RequestHandler<TEntity>> _logger;

    private PropertyInfo _keyInfo;

    public RequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<RequestHandler<TEntity>> logger)
    {
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
        //var type = typeof(TEntity).GetProperties().CustomAttributes.SingleOrDefault(attr => attr.AttributeType == typeof(KeyAttribute));
        _keyInfo = typeof(TEntity).GetProperties().FirstOrDefault(p =>
            p.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));
        // foreach(var t in type)
        // {
        //     _logger.LogError(t.Name);
        //     _logger.LogError("Normal Attributes:");

        //     foreach(var attr in t.CustomAttributes)
        //     {
        //         logger.LogError(attr.AttributeType.Name);
        //     }


        // }
       // _logger.LogError(type.Name);



    }

    public async Task<HttpResponseData> HandleRequest(HttpRequestData req, string? key = null)
    {
        //DataServiceResponse<string>
        _logger.LogInformation("Http Request Method of type {method} has been received", req.Method);

        switch (req.Method)
        {
            case "GET":
                if(key != null)
                {

                    return CreateHttpResponse(req, await getById(req, keyPredicate));
                }
                else
                {
                    return CreateHttpResponse(req, await Get(req));
                }
            case "DELETE":
                if(key != null)
                if (keyPredicate != null)
                {
                    return await DeleteById(req,key);
                    return CreateHttpResponse(req, await DeleteById(req, keyPredicate));
                }
                else
                {
                    return createErrorResponse(req);
                }
            case "POST":
                return CreateHttpResponse(req, await Post(req));
            case "PUT":
                if (keyPredicate != null)
                {
                    return await UpdateById(req,key);
                    return CreateHttpResponse(req, await DeleteById(req, keyPredicate));
                }
                else
                {
                    return createErrorResponse(req);
                }
            default:
                return CreateHttpResponse(req, null, HttpStatusCode.MethodNotAllowed);
        }



    }

    private async Task<DataServiceResponse<string>> Get(HttpRequestData req)
    {
        var predicate = CreateFilterExpression(req);
        var result = await _dataServiceAccessor.GetRange(predicate);

        return new DataServiceResponse<string>
        {
            JsonData = JsonSerializer.Serialize(result)
        };
    }

    private async Task<DataServiceResponse<string>> getById(HttpRequestData req, string keyValue)
    {


        var keyPredicate = CreateGetByKeyExpression(keyValue);

        _logger.LogError(keyPredicate.ToString());
        var result = await _dataServiceAccessor.GetSingle(keyPredicate);



        return new DataServiceResponse<string>
        {
            JsonData = JsonSerializer.Serialize(result)
        };

    }

    private async Task<DataServiceResponse<string>> Post(HttpRequestData req)
    {
        try
        {
            var entityData = await getBodyFromRequest(req);

            var result = await _dataServiceAccessor.InsertSingle(entityData);
            if (!result)
            {
                return new DataServiceResponse<string>
                {
                    ErrorMessage = "Failed to Insert Record"
                };
            }
            return new DataServiceResponse<string>
            {
                JsonData = "Success"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get request Data, This is due to a badly formed request");
            return new DataServiceResponse<string>
            {
                ErrorMessage = "Bad Request"
            };
        }


    }

    private async Task<DataServiceResponse<string>> UpdateById(HttpRequestData req, string key)
    {
        try
        {
            var entityData = await getBodyFromRequest(req);

            var result = await _dataServiceAccessor.Update(entityData);
            if (!result)
            {
                return new DataServiceResponse<string>
                {
                    ErrorMessage = "Failed to Insert Record"
                };
            }
            return new DataServiceResponse<string>
            {
                JsonData = "Success"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Updating Record ");
            return new DataServiceResponse<string>
            {
                ErrorMessage = "Bad Request"
            };
        }
    }

    private async Task<DataServiceResponse<string>> DeleteById(HttpRequestData req, string key)
    {

        var keyPredicate = CreateGetByKeyExpression(key);
        var result = await _dataServiceAccessor.Remove(keyPredicate);

        return new DataServiceResponse<string>
        {
            JsonData = JsonSerializer.Serialize(result)
        };

    }

    private async Task<TEntity> getBodyFromRequest(HttpRequestData req)
    {
        string jsonData;
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            jsonData = await reader.ReadToEndAsync();
        }
        return JsonSerializer.Deserialize<TEntity>(jsonData);

    }

    private Expression<Func<TEntity,bool>> CreateGetByKeyExpression(string filter)
    {
        string keyName = GetKeyName(typeof(TEntity));
        var entityParameter = Expression.Parameter(typeof(TEntity));
        var entityKey = Expression.Property(entityParameter,keyName);
        var filterConstant = Expression.Constant(Convert.ChangeType(filter,GetPropertyType(typeof(TEntity),keyName)));

        var expr = Expression.Equal(entityKey,filterConstant);

        _logger.LogError(expr.Print());
       return Expression.Lambda<Func<TEntity,bool>>(expr,entityParameter);
    }

    private Expression<Func<TEntity,bool>> CreateFilterExpression(HttpRequestData req)
    {
        var entityParameter = Expression.Parameter(typeof(TEntity));
        BinaryExpression expr = null;
        if(req.Query.AllKeys.IsNullOrEmpty())
        {
            Expression<Func<TEntity, bool>> expression = i => true;
            return expression;
        }
        foreach(var item in req.Query.AllKeys){
            _logger.LogInformation($"item {item} data: {req.Query[item]}");

            if(!PropertyExists(typeof(TEntity),item))
            {
                _logger.LogWarning("Query Item: '{item}' does not exist in TEntity: '{entityName}'",item,typeof(TEntity).Name);
                continue;
            }
            var entityKey = Expression.Property(entityParameter,item);
            var filterConstant = Expression.Constant(Convert.ChangeType(req.Query[item],GetPropertyType(typeof(TEntity),item)));
            var comparison  = Expression.Equal(entityKey,filterConstant);
            if(expr == null){
                expr = comparison;
                continue;
            }
            expr = Expression.AndAlso(expr,comparison);
        }
        _logger.LogError(expr.Print());
        return Expression.Lambda<Func<TEntity,bool>>(expr,entityParameter);;
    }


    private string GetKeyName(Type type)
    {
        _keyInfo = type.GetProperties().FirstOrDefault(p =>
            p.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));

        return _keyInfo.Name;
    }

    private Type GetPropertyType(Type type, string property)
    {
        return type.GetProperty(property).PropertyType;
    }

    private bool PropertyExists(Type type,string property) =>
        Array.Exists(type.GetProperties(),p => p.Name == property);

    }


    private HttpResponseData createErrorResponse(HttpRequestData req)
    {
        var errorResponse = new DataServiceResponse<string> { ErrorMessage = "No Key was Provided for deletion" };
        return CreateHttpResponse(req, errorResponse);
    }

    private HttpResponseData CreateHttpResponse(HttpRequestData req, DataServiceResponse<string> dataServiceResponse, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
    {
        HttpStatusCode statusCode;
        byte[] responseBody = null!;
        if (dataServiceResponse.ErrorMessage == null)
        {
            statusCode = HttpStatusCode.OK;
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.JsonData);
        }
        else if (string.IsNullOrWhiteSpace(dataServiceResponse.JsonData))
        {
            responseBody = Encoding.UTF8.GetBytes("");
            statusCode = HttpStatusCode.NoContent;
        }
        else
        {
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.ErrorMessage);
            statusCode = httpStatusCode;
        }

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");


        response.Body = new MemoryStream(responseBody);
        return response;
    }

    // private Func<TEntity,bool> predicate(TEntity entity,string key)
    // {

    //     var datatype = Convert<typeof(_keyInfo)>(key);

    //     return Expression.Equal(Expression.)
    // }

    // public static T Convert<T>(string input)
    // {
    //     var converter = TypeDescriptor.GetConverter(typeof(T));
    //     if(converter != null)
    //     {
    //         //Cast ConvertFromString(string text) : object to (T)
    //         return (T)converter.ConvertFromString(input);
    //     }
    //     return default(T);
    // }
}
