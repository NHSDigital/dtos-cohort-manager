namespace DataServices.Client;

using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class DataServiceClientBuilder
{
    private readonly IHostBuilder _hostBuilder;
    private readonly Dictionary<Type, string> _dataServiceUrls;
    private bool CacheRequired = false;
    public DataServiceClientBuilder(IHostBuilder hostBuilder)
    {
        _dataServiceUrls = new();
        _hostBuilder = hostBuilder;
    }

    public DataServiceClientBuilder AddDataService<TEntity>(string url) where TEntity : class
    {
        _hostBuilder.ConfigureServices(_ => {
            _.AddTransient<IDataServiceClient<TEntity>,DataServiceClient<TEntity>>();

        });
        _dataServiceUrls.Add(typeof(TEntity),url);

        return this;
    }

    public DataServiceClientBuilder AddCachedDataService<TEntity>(string url) where TEntity : class
    {
        CacheRequired = true;
        _hostBuilder.ConfigureServices(_ => {
            _.AddTransient<IDataServiceClient<TEntity>,DataServiceCacheClient<TEntity>>();

        });
        _dataServiceUrls.Add(typeof(TEntity),url);

        return this;
    }


    public IHostBuilder Build()
    {
        DataServiceResolver dataServiceResolver = new DataServiceResolver(_dataServiceUrls);
        _hostBuilder.ConfigureServices(_ =>{
            if(CacheRequired){
                _.AddMemoryCache();
            }
            _.AddSingleton(dataServiceResolver);
            _.AddSingleton<ICallFunction, CallFunction>();
        });
        return _hostBuilder;
    }

}