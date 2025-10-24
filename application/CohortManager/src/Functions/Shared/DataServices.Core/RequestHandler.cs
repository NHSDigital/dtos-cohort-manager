
namespace DataServices.Core;

using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Hl7.Fhir.Utility;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class RequestHandler<TEntity> : IRequestHandler<TEntity> where TEntity : class
{

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    private readonly ILogger<RequestHandler<TEntity>> _logger;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly PropertyInfo _keyInfo;
    private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

    private const string UnauthorizedErrorMessage = "Action was either Unauthorized or not enabled";
    private const string SuccessMessage = "Success";
    private const string DeserializationErrorMessage = "Failed to deserialize Record";

    public RequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<RequestHandler<TEntity>> logger, AuthenticationConfiguration authenticationConfiguration)
    {
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
        _authConfig = authenticationConfiguration;
        _keyInfo = ReflectionUtilities.GetKey<TEntity>();
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
                    return CreateErrorResponse(req,"No Key Provided for Deletion",HttpStatusCode.BadRequest);
                }
            case "POST":
                // Check if this is an upsert request (special key "upsert")
                if (key != null && key.Equals("upsert", StringComparison.OrdinalIgnoreCase))
                {
                    return await Upsert(req);
                }
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

        try
        {
            var predicate = CreateFilterExpression(req);
            object result;

            if (GetBooleanQueryItem(req,"single"))
            {
                result = await _dataServiceAccessor.GetSingle(predicate);
            }
            else
            {
                result = await _dataServiceAccessor.GetRange(predicate);
            }

            if (!ResultHasContent(result))
            {
                return CreateErrorResponse(req,"No Data Found",HttpStatusCode.NoContent);
            }

            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = JsonSerializer.Serialize(result)
            });
        }
        catch(MultipleRecordsFoundException mre)
        {
            _logger.LogWarning(mre,"Multiple Records were returned from filter expression when only one was expected: {Message}",mre.Message);
            return CreateErrorResponse(req,"Multiple rows met filter condition when only one row was expected",HttpStatusCode.BadRequest);


        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"Unable to parse filter expression");
            return CreateErrorResponse(req,"Unable to parse filter Expression",HttpStatusCode.BadRequest);
        }

    }

    private async Task<HttpResponseData> GetById(HttpRequestData req, string keyValue)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while trying to get data");
            return CreateErrorResponse(req, "Failed to get Record", HttpStatusCode.InternalServerError);
        }

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
                JsonData = SuccessMessage
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return CreateErrorResponse(req, DeserializationErrorMessage, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while trying to add data");
            return CreateErrorResponse(req,"Failed to Insert Record",HttpStatusCode.InternalServerError);
        }


    }

    private async Task<HttpResponseData> Upsert(HttpRequestData req)
    {
        if(!_authConfig.CanPost(req)) // Reuse Post authorization for Upsert
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

            var entityData = JsonSerializer.Deserialize<TEntity>(jsonData,jsonSerializerOptions);
            if (entityData == null)
            {
                return CreateErrorResponse(req, DeserializationErrorMessage, HttpStatusCode.BadRequest);
            }

            // Get the key value from the entity to create the predicate
            var keyValue = _keyInfo.GetValue(entityData);
            if (keyValue == null)
            {
                return CreateErrorResponse(req, "Entity key is null", HttpStatusCode.BadRequest);
            }

            var keyPredicate = CreateGetByKeyExpression(keyValue.ToString());
            var result = await _dataServiceAccessor.Upsert(entityData, keyPredicate);

            if (!result)
            {
                return CreateErrorResponse(req,"Failed to Upsert Record",HttpStatusCode.InternalServerError);
            }

            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = SuccessMessage
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to deserialize Data, This is due to a badly formed request");
            return CreateErrorResponse(req, DeserializationErrorMessage, HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while trying to upsert data");
            return CreateErrorResponse(req,"Failed to Upsert Record",HttpStatusCode.InternalServerError);
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
            if (entityData == null)
                return CreateErrorResponse(req, "Couldn't deserialise body", HttpStatusCode.NotFound);
            var keyPredicate = CreateGetByKeyExpression(key);

            var result = await _dataServiceAccessor.Update(entityData, keyPredicate);
            if (result == null)
            {
                return CreateErrorResponse(req,"Record not found",HttpStatusCode.NotFound);
            }
            return CreateHttpResponse(req,new DataServiceResponse<string>
            {
                JsonData = SuccessMessage
            });
        }
        catch(JsonException je)
        {
            _logger.LogError(je, "Failed to get deserialize Data, This is due to a badly formed request");
            return CreateErrorResponse(req, DeserializationErrorMessage, HttpStatusCode.BadRequest);
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
            JsonData = SuccessMessage
        });

    }

    private static bool IsJsonArray(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Array;
    }

    private Expression<Func<TEntity, bool>> CreateGetByKeyExpression(string filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter), "Filter parameter cannot be null");
        }

        var entityParameter = Expression.Parameter(typeof(TEntity));
        var entityKey = Expression.Property(entityParameter, _keyInfo.Name);
        var filterConstant = Expression.Constant(Convert.ChangeType(filter, ReflectionUtilities.GetPropertyType(typeof(TEntity), _keyInfo.Name)));
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

    private static bool GetBooleanQueryItem(HttpRequestData req, string headerKey, bool defaultValue = false)
    {
        if(req.Query[headerKey] == null){
            return defaultValue;
        }
        if(bool.TryParse(req.Query[headerKey],out var result)){
            return result;
        }
        return defaultValue;
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
