using System.Formats.Tar;
using System.Text.Json;
using DataServices.Database;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Logging;

public class RequestHandler<TEntity> : IRequestHandler<TEntity> where TEntity : class
{

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;

    private ILogger<RequestHandler<TEntity>> _logger;


    public RequestHandler(IDataServiceAccessor<TEntity> dataServiceAccessor, ILogger<RequestHandler<TEntity>> logger)
    {
        _dataServiceAccessor = dataServiceAccessor;
        _logger = logger;
    }

    public async Task<DataServiceResponse<string>> HandleRequest(HttpRequestData req, Func<TEntity,bool> keyPredicate)
    {

        _logger.LogInformation("Http Request Method of type {method} has been received",req.Method);

        switch(req.Method)
        {
            case "GET":
                if(keyPredicate != null){
                    return await getById(req,keyPredicate);
                }
                else{
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

    private async Task<DataServiceResponse<string>> getById(HttpRequestData httpRequestMessage, Func<TEntity,bool> keyPredicate)
    {
        var result = await _dataServiceAccessor.GetSingle(keyPredicate);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };

    }

    private async Task<DataServiceResponse<string>> DeleteById(HttpRequestData httpRequestMessage, Func<TEntity,bool> keyPredicate)
    {
        var result = await _dataServiceAccessor.Remove(keyPredicate);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };

    }
}
