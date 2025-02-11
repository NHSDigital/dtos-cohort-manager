
namespace DataServices.Core;

using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class RequestHandler<TEntity> : IRequestHandler<TEntity> where TEntity : class
{

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    private readonly ILogger<RequestHandler<TEntity>> _logger;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly PropertyInfo _keyInfo;
    private readonly IGetRequestHandler<TEntity> _getRequestHandler;
    private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

    private const string UnauthorizedErrorMessage = "Action was either Unauthorized or not enabled";

    public RequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<RequestHandler<TEntity>> logger, AuthenticationConfiguration authenticationConfiguration, IGetRequestHandler<TEntity> getRequestHandler)
    {
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
        _authConfig = authenticationConfiguration;
        _keyInfo = ReflectionUtilities.GetKey<TEntity>();
        _getRequestHandler = getRequestHandler;
    }

    public async Task<HttpResponseData> HandleRequest(HttpRequestData req, string? key = null)
    {
        _logger.LogInformation("Http Request Method of type {Method} has been received", req.Method);
        _logger.LogInformation("DataService of type {Type} has been called",typeof(TEntity) );

        switch (req.Method)
        {
            case "GET":
                if (key != null)
                {
                    return await GetById(req, key);
                }
                else
                {
                    return await Get(req);
                }
            case "DELETE":
                if (key != null)
                {
                    return await DeleteById(req, key);
                }
                else
                {
                    return HttpHelpers.CreateErrorResponse(req,"No Key Provided for Deletion",HttpStatusCode.BadRequest);
                }
            case "POST":
                return await Post(req);
            case "PUT":
                if (key != null)
                {
                    return await UpdateById(req, key);
                }
                else
                {
                    return HttpHelpers.CreateErrorResponse(req,"No Key Provided for Put",HttpStatusCode.BadRequest);
                }
            default:
                return HttpHelpers.CreateHttpResponse(req, null, HttpStatusCode.MethodNotAllowed);
        }



    }

    private async Task<HttpResponseData> Get(HttpRequestData req)
    {

        if(!_authConfig.CanGet(req))
        {
            _logger.LogWarning("Unauthorized Method was called");
            return HttpHelpers.CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }

        try
        {
            return await _getRequestHandler.Get(req);

            // var predicate = CreateFilterExpression(req);
            // object result;

            // if (HttpHelpers.GetBooleanQueryItem(req,"single"))
            // {
            //     result = await _dataServiceAccessor.GetSingle(predicate);
            // }
            // else
            // {
            //     result = await _dataServiceAccessor.GetRange(predicate);
            // }

            // if (!ResultHasContent(result))
            // {
            //     return HttpHelpers.CreateErrorResponse(req,"No Data Found",HttpStatusCode.NoContent);
            // }

            // return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>
            // {
            //     JsonData = JsonSerializer.Serialize(result)
            // });
        }
        catch(MultipleRecordsFoundException mre)
        {
            _logger.LogWarning(mre,"Multiple Records were returned from filter expression when only one was expected: {Message}",mre.Message);
            return HttpHelpers.CreateErrorResponse(req,"Multiple rows met filter condition when only one row was expected",HttpStatusCode.BadRequest);


        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"Unable to parse filter expression");
            return HttpHelpers.CreateErrorResponse(req,"Unable to parse filter Expression",HttpStatusCode.BadRequest);
        }

    }

    private async Task<HttpResponseData> GetById(HttpRequestData req, string keyValue)
    {
        if(!_authConfig.CanGetById(req))
        {
            return HttpHelpers.CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }

        var keyPredicate = FilterHelpers.CreateGetByKeyExpression<TEntity>(keyValue,_keyInfo.Name);
        var result = await _dataServiceAccessor.GetSingle(keyPredicate);

        if(result == null)
        {
            return HttpHelpers.CreateErrorResponse(req,"No Data Found",HttpStatusCode.NotFound);
        }


        return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>
        {
            JsonData = JsonSerializer.Serialize(result)
        });

    }

    private async Task<HttpResponseData> Post(HttpRequestData req)
    {
        if(!_authConfig.CanPost(req))
        {
            return HttpHelpers.CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        try
        {

            string jsonData;
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                jsonData = await reader.ReadToEndAsync();
            }

            bool result;
            if(IsJsonArray(jsonData))
            {
                var entityData = JsonSerializer.Deserialize<List<TEntity>>(jsonData,jsonSerializerOptions);
                result = await _dataServiceAccessor.InsertMany(entityData);
            }
            else
            {
                var entityData = JsonSerializer.Deserialize<TEntity>(jsonData,jsonSerializerOptions);
                result = await _dataServiceAccessor.InsertSingle(entityData);
            }


            if (!result)
            {

                return HttpHelpers.CreateErrorResponse(req,"Failed to Insert Record",HttpStatusCode.InternalServerError);
            }
            return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = "Success"
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return HttpHelpers.CreateErrorResponse(req,"Failed to deserialize Record",HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while trying to add data");
            return HttpHelpers.CreateErrorResponse(req,"Failed to Insert Record",HttpStatusCode.InternalServerError);
        }


    }

    private async Task<HttpResponseData> UpdateById(HttpRequestData req, string key)
    {
        if(!_authConfig.CanPut(req))
        {
            return HttpHelpers.CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        try
        {
            string jsonData;
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8)){
                jsonData = await reader.ReadToEndAsync();
            }


            var entityData = JsonSerializer.Deserialize<TEntity>(jsonData,jsonSerializerOptions);
            if (entityData == null)
                return HttpHelpers.CreateErrorResponse(req, "Couldn't deserialise body", HttpStatusCode.NotFound);
            var keyPredicate = FilterHelpers.CreateGetByKeyExpression<TEntity>(key,_keyInfo.Name);

            var result = await _dataServiceAccessor.Update(entityData, keyPredicate);
            if (result == null)
            {
                return HttpHelpers.CreateErrorResponse(req,"Record not found",HttpStatusCode.NotFound);
            }
            return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = "Success"
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return HttpHelpers.CreateErrorResponse(req,"Failed to deserialize Record",HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Update Record");
            return HttpHelpers.CreateErrorResponse(req,"Failed to Update Record",HttpStatusCode.BadRequest);
        }
    }

    private async Task<HttpResponseData> DeleteById(HttpRequestData req, string key)
    {
        if(!_authConfig.CanDelete(req))
        {
            return HttpHelpers.CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        var keyPredicate = FilterHelpers.CreateGetByKeyExpression<TEntity>(key,_keyInfo.Name);
        var result = await _dataServiceAccessor.Remove(keyPredicate);
        if(!result)
        {
            return HttpHelpers.CreateErrorResponse(req,"Failed to delete Record",HttpStatusCode.NotFound);
        }
        return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>
        {
            JsonData = "Success"
        });

    }

    private static bool IsJsonArray(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Array;
    }



    private Expression<Func<TEntity, bool>> CreateFilterExpression(HttpRequestData req)
    {
        var entityParameter = Expression.Parameter(typeof(TEntity));
        BinaryExpression expr = null;
        if (req.Query.AllKeys.IsNullOrEmpty())
        {
            Expression<Func<TEntity, bool>> expression = i => true;
            return expression;
        }
        foreach (var item in req.Query.AllKeys)
        {
            if(item == "query")
            {
                return DynamicExpressionParser.ParseLambda<TEntity,bool>(new ParsingConfig(),true, req.Query[item]);
            }

            if (!ReflectionUtilities.PropertyExists(typeof(TEntity), item))
            {
                _logger.LogWarning("Query Item: '{Item}' does not exist in TEntity: '{EntityName}'", item, typeof(TEntity).Name);
                continue;
            }
            var entityKey = Expression.Property(entityParameter, item);
            var filterConstant = Expression.Constant(Convert.ChangeType(req.Query[item], ReflectionUtilities.GetPropertyType(typeof(TEntity), item)));
            var comparison = Expression.Equal(entityKey, filterConstant);
            if (expr == null)
            {
                expr = comparison;
                continue;
            }
            expr = Expression.AndAlso(expr, comparison);
        }
        var lambdaexpr = Expression.Lambda<Func<TEntity, bool>>(expr, entityParameter);
        return lambdaexpr;

    }



    private static bool ResultHasContent(Object obj)
    {
        if(obj == null)
        {
            return true;
        }

        if(obj is not IEnumerable<TEntity>) // Object isnt null and isnt IEnumerable so will have data
        {
            return true;
        }

        var data = (IEnumerable<TEntity>)obj;
        var result = data.Any();

        return result;
    }

}
