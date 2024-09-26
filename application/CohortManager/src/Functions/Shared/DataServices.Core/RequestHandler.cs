using System.Formats.Tar;
using System.Text.Json;
using DataServices.Database;
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

    public async Task<DataServiceResponse<string>> HandleRequest(HttpRequestMessage httpRequestMessage, Func<TEntity,bool> keyPredicate)
    {

        // _logger.LogInformation("Http Request Method of type {method} has been received",httpRequestMessage.Method);
        if(keyPredicate != null){
            return await getById(httpRequestMessage,keyPredicate);
        }
        else{
            return await Get(httpRequestMessage);
        }


    }

    private async Task<DataServiceResponse<string>> Get(HttpRequestMessage httpRequestMessage)
    {
        var result = await _dataServiceAccessor.GetRange(i => true);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };
    }

    private async Task<DataServiceResponse<string>> getById(HttpRequestMessage httpRequestMessage, Func<TEntity,bool> keyPredicate)
    {

        var result = await _dataServiceAccessor.GetSingle(keyPredicate);

        return new DataServiceResponse<string>{
            JsonData = JsonSerializer.Serialize(result)
        };

    }
}
