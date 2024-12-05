namespace DataServices.Client;

using System.Data;
using System.Xml.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class DataServiceClientExtension
{
    public static DataServiceClientBuilder AddDataServicesHandler(this IHostBuilder hostBuilder)
    {
        return new DataServiceClientBuilder(hostBuilder);
    }
    public static IHostBuilder AddDataServicesHandler<TEntity>(this IHostBuilder hostBuilder, string url) where TEntity : class
    {
        hostBuilder.ConfigureServices(_ => {
            _.AddTransient<IDataServiceClient<TEntity>,DataServiceClient<TEntity>>();
        });
        return hostBuilder;
    }
}
