
namespace DataServices.Core;

using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class RequestHandler<TEntity> : IRequestHandler<TEntity> where TEntity : class
{

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    private readonly ILogger<RequestHandler<TEntity>> _logger;

    private readonly AuthenticationConfiguration _authConfig;

    private readonly PropertyInfo _keyInfo;

    private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions{
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

    private const string UnauthorizedErrorMessage = "Action was either Unauthorized or not enabled";

    public RequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<RequestHandler<TEntity>> logger, AuthenticationConfiguration authenticationConfiguration)
    {
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
        _authConfig = authenticationConfiguration;

        var type = typeof(TEntity);
        _keyInfo = type.GetProperties().FirstOrDefault(p =>
            p.CustomAttributes.Any(attr => attr.AttributeType == typeof(KeyAttribute)));

    }

    public async Task<HttpResponseData> HandleRequest(HttpRequestData req, string? key = null)
    {
        _logger.LogInformation("Http Request Method of type {method} has been received", req.Method);
        _logger.LogInformation("DataService of type {type} has been called",typeof(TEntity) );

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
                    return CreateErrorResponse(req,"No Key Provided for Deletion",HttpStatusCode.BadRequest);
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
                    return CreateErrorResponse(req,"No Key Provided for Put",HttpStatusCode.BadRequest);
                }
            default:
                return CreateHttpResponse(req, null, HttpStatusCode.MethodNotAllowed);
        }



    }

    private async Task<HttpResponseData> Get(HttpRequestData req)
    {

        if(!_authConfig.CanGet(req))
        {
            _logger.LogWarning("Unauthorized Method was called");
            return CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        try{
            var predicate = CreateFilterExpression(req);
            var result = await _dataServiceAccessor.GetRange(predicate);
            if(result == null || !result.Any())
            {
                return CreateErrorResponse(req,"No Data Found",HttpStatusCode.NoContent);
            }
            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = JsonSerializer.Serialize(result)
            });
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"Unable to parse filter expression");
            return CreateErrorResponse(req,"Unable to parse filter Expression",HttpStatusCode.BadRequest);
        }

    }

    private async Task<HttpResponseData> GetById(HttpRequestData req, string keyValue)
    {
        if(!_authConfig.CanGetById(req))
        {
            return CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }

        var keyPredicate = CreateGetByKeyExpression(keyValue);
        var result = await _dataServiceAccessor.GetSingle(keyPredicate);

        if(result == null)
        {
            return CreateErrorResponse(req,"No Data Found",HttpStatusCode.NotFound);
        }


        return CreateHttpResponse(req,new DataServiceResponse<string>
        {
            JsonData = JsonSerializer.Serialize(result)
        });

    }

    private async Task<HttpResponseData> Post(HttpRequestData req)
    {
        if(!_authConfig.CanPost(req))
        {
            return CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
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

                return CreateErrorResponse(req,"Failed to Insert Record",HttpStatusCode.InternalServerError);
            }
            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = "Success"
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return CreateErrorResponse(req,"Failed to deserialize Record",HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while trying to add data");
            return CreateErrorResponse(req,"Failed to Insert Record",HttpStatusCode.InternalServerError);
        }


    }

    private async Task<HttpResponseData> UpdateById(HttpRequestData req, string key)
    {
        if(!_authConfig.CanPut(req))
        {
            return CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        try
        {
            string jsonData;
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8)){
                jsonData = await reader.ReadToEndAsync();
            }


            var entityData = JsonSerializer.Deserialize<TEntity>(jsonData,jsonSerializerOptions);
            var keyPredicate = CreateGetByKeyExpression(key);

            var result = await _dataServiceAccessor.Update(entityData, keyPredicate);
            if (result == null)
            {
                return CreateErrorResponse(req,"Record not found",HttpStatusCode.NotFound);
            }
            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = "Success"
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return CreateErrorResponse(req,"Failed to deserialize Record",HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Update Record");
            return CreateErrorResponse(req,"Failed to Update Record",HttpStatusCode.BadRequest);
        }
    }

    private async Task<HttpResponseData> DeleteById(HttpRequestData req, string key)
    {
        if(!_authConfig.CanDelete(req))
        {
            return CreateErrorResponse(req,UnauthorizedErrorMessage,HttpStatusCode.Unauthorized);
        }
        var keyPredicate = CreateGetByKeyExpression(key);
        var result = await _dataServiceAccessor.Remove(keyPredicate);
        if(!result)
        {
            return CreateErrorResponse(req,"Failed to delete Record",HttpStatusCode.NotFound);
        }
        return CreateHttpResponse(req,new DataServiceResponse<string>
        {
            JsonData = "Success"
        });

    }

    private static bool IsJsonArray(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Array;
    }

    private Expression<Func<TEntity, bool>> CreateGetByKeyExpression(string filter)
    {
        var entityParameter = Expression.Parameter(typeof(TEntity));
        var entityKey = Expression.Property(entityParameter, _keyInfo.Name);
        var filterConstant = Expression.Constant(Convert.ChangeType(filter, GetPropertyType(typeof(TEntity), _keyInfo.Name)));
        var expr = Expression.Equal(entityKey, filterConstant);

        return Expression.Lambda<Func<TEntity, bool>>(expr, entityParameter);
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

            if (!PropertyExists(typeof(TEntity), item))
            {
                _logger.LogWarning("Query Item: '{item}' does not exist in TEntity: '{entityName}'", item, typeof(TEntity).Name);
                continue;
            }
            var entityKey = Expression.Property(entityParameter, item);
            var filterConstant = Expression.Constant(Convert.ChangeType(req.Query[item], GetPropertyType(typeof(TEntity), item)));
            var comparison = Expression.Equal(entityKey, filterConstant);
            if (expr == null)
            {
                expr = comparison;
                continue;
            }
            expr = Expression.AndAlso(expr, comparison);
        }
        _logger.LogError(expr.Print());
        var lambdaexpr = Expression.Lambda<Func<TEntity, bool>>(expr, entityParameter);
        lambdaexpr.Print();
        return lambdaexpr;

    }

    private static Type GetPropertyType(Type type, string property)
    {
        return type.GetProperty(property).PropertyType;
    }

    private static bool PropertyExists(Type type, string property) =>
        Array.Exists(type.GetProperties(), p => p.Name == property);





    private HttpResponseData CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
    {
        var errorResponse = new DataServiceResponse<string> { ErrorMessage = message };
        return CreateHttpResponse(req, errorResponse, statusCode);
    }

    private static HttpResponseData CreateHttpResponse(HttpRequestData req, DataServiceResponse<string> dataServiceResponse, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
    {
        HttpStatusCode statusCode;
        byte[] responseBody = null!;
        if(httpStatusCode == HttpStatusCode.NoContent){
            responseBody = Encoding.UTF8.GetBytes("");
            statusCode = httpStatusCode;
        }
        else if (dataServiceResponse.ErrorMessage == null)
        {
            statusCode = HttpStatusCode.OK;
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.JsonData);
        }
        else if (dataServiceResponse.ErrorMessage != null)
        {
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.ErrorMessage);
            statusCode = httpStatusCode;
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


}