using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using DataServices.Database;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging;

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

    public async Task<DataServiceResponse<string>> HandleRequest(HttpRequestData req, Func<TEntity,bool> keyPredicate)
    {

        _logger.LogInformation("Http Request Method of type {method} has been received",req.Method);

        switch(req.Method)
        {
            case "GET":
                if(keyPredicate != null)
                {
                    return await getById(req,keyPredicate);
                }
                else
                {
                    return await Get(req);
                }
            case "DELETE":
                if(keyPredicate != null)
                {
                    return await DeleteById(req,keyPredicate);
                }
                else
                {
                    return new DataServiceResponse<string>{ErrorMessage = "No Key was Provided for deletion"};
                }
            case "POST":
                return await Post(req);
            case "PUT":
                if(keyPredicate != null)
                {
                    return await DeleteById(req,keyPredicate);
                }
                else
                {
                    return new DataServiceResponse<string>{ErrorMessage = "No Key was Provided for Update"};
                }
            default :
                throw new NotImplementedException();
        }



    }

    private async Task<DataServiceResponse<string>> Get(HttpRequestData req)
    {
        var result = await _dataServiceAccessor.GetRange(i => true);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };
    }

    private async Task<DataServiceResponse<string>> getById(HttpRequestData req, Func<TEntity,bool> keyPredicate)
    {
        var result = await _dataServiceAccessor.GetSingle(keyPredicate);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };

    }

    private async Task<DataServiceResponse<string>> Post(HttpRequestData req)
    {
        try
        {
            var entityData = await getBodyFromRequest(req);

            var result = await _dataServiceAccessor.InsertSingle(entityData);
            if(!result)
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
        catch(Exception ex)
        {
            _logger.LogError(ex,"Failed to get request Data, This is due to a badly formed request");
            return new DataServiceResponse<string>{
                ErrorMessage = "Bad Request"
            };
        }


    }

    private async Task<DataServiceResponse<string>> UpdateById(HttpRequestData req, Func<TEntity,bool> keyPredicate)
    {
        try
        {
            var entityData = await getBodyFromRequest(req);

            var result = await _dataServiceAccessor.Update(entityData);
            if(!result)
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
        catch(Exception ex)
        {
            _logger.LogError(ex,"Error Updating Record ");
            return new DataServiceResponse<string>{
                ErrorMessage = "Bad Request"
            };
        }
    }

    private async Task<DataServiceResponse<string>> DeleteById(HttpRequestData req, Func<TEntity,bool> keyPredicate)
    {
        var result = await _dataServiceAccessor.Remove(keyPredicate);

        return new DataServiceResponse<string>{
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
