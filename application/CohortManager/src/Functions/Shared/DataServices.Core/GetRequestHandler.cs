namespace DataServices.Core;

using System.Linq.Dynamic.Core;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class GetRequestHandler<TEntity> : IGetRequestHandler<TEntity> where TEntity : class
{
    private readonly PropertyInfo _keyInfo;
    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    private readonly ILogger<GetRequestHandler<TEntity>> _logger;
    private readonly IGetRequestAccessor<TEntity> _getRequestAccessor;
    public GetRequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<GetRequestHandler<TEntity>> logger, IGetRequestAccessor<TEntity> getRequestAccessor)
    {
        _keyInfo = ReflectionUtilities.GetKey<TEntity>();
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
        _getRequestAccessor = getRequestAccessor;
    }

    public async Task<HttpResponseData> Get(HttpRequestData req)
    {
        var filterRequest = BuildFilterRequest(req);
        var result = await _getRequestAccessor.Get(filterRequest);
        if(result == null){
            return HttpHelpers.CreateErrorResponse(req,"No Data found",HttpStatusCode.NoContent);
        }
        var resultjson = JsonSerializer.Serialize(result);
        return HttpHelpers.CreateHttpResponse(req,new DataServiceResponse<string>{
            JsonData = resultjson
        },HttpStatusCode.OK);

    }

    public async Task<HttpResponseData> GetByIdAsync(HttpRequestData req, string keyValue)
    {
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

    private FilterRequest<TEntity> BuildFilterRequest(HttpRequestData req)
    {
        var filterRequest = new FilterRequest<TEntity>();
        filterRequest.Limit = 0;
        filterRequest.Includes = new List<string>();
        foreach (var item in req.Query.AllKeys)
        {
            switch(item){
                case "Query":
                    filterRequest.Where = DynamicExpressionParser.ParseLambda<TEntity,bool>(new ParsingConfig(),true, req.Query[item]);
                    break;
                case "Include":
                    filterRequest.Includes.AddRange([.. req.Query[item].Split(",")]);
                    break;
                case "Limit":
                    filterRequest.Limit = int.Parse(req.Query[item]);
                    break;
                case "Single":
                    filterRequest.Single = HttpHelpers.GetBooleanQueryItem(req,item,false);
                    break;
                default:
                    _logger.LogWarning("unrecognized Query parameter : {} received",item);
                    break;
            }
        }

        return filterRequest;


    }


}
