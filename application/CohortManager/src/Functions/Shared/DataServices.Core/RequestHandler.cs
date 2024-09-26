using System.Formats.Tar;
using DataServices.Database;

public class RequestHandler<TEntity>
{

    private readonly DataServicesContext _dataServicesContext;

    private readonly IDataServiceAccessor<TEntity> _dataServiceAccessor;
    public RequestHandler(DataServicesContext dataServicesContext)
    {

        _dataServicesContext = dataServicesContext;
    }

    public DataServiceResponse<TEntity> HandleRequest(HttpRequestMessage httpRequestMessage, string? Key)
    {

    }

    private async Task<DataServiceResponse<TEntity>> Get(HttpRequestMessage httpRequestMessage)
    {
        throw new NotImplementedException();
    }

    private async Task<DataServiceResponse<TEntity>> getById(HttpRequestMessage httpRequestMessage)
    {
        throw new NotImplementedException();
    }
}
