using System.Formats.Tar;
using DataServices.Database;

public class RequestHandler<TEntity>
{

    private readonly DataServicesContext _dataServicesContext;
    public RequestHandler(DataServicesContext dataServicesContext)
    {
        _dataServicesContext = dataServicesContext;
    }

    public DataServiceResponse<TEntity> HandleRequest(HttpRequestMessage httpRequestMessage)
    {
        switch(*)

    }

    private async Task<DataServiceResponse<TEntity>> Get(HttpRequestMessage httpRequestMessage)
    {

    }

    private async Task<DataServiceResponse<TEntity>> getById(HttpRequestMessage httpRequestMessage)
    {

    }
}
