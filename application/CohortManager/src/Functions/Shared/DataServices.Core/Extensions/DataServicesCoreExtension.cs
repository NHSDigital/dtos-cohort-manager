namespace DataServices.Core;

using Common;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public static class DataServicesCoreExtension
{
    public static IHostBuilder AddDataServicesHandler(this IHostBuilder hostBuilder, string? connectionString = null)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddDbContext<DataServicesContext>(
                options => options.UseSqlServer(connectionString ?? Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"))

            );
            _.AddSingleton<ICreateResponse, CreateResponse>();
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IRequestHandler<>), typeof(RequestHandler<>)));
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IDataServiceAccessor<>), typeof(DataServiceAccessor<>)));

        });
    }

}
