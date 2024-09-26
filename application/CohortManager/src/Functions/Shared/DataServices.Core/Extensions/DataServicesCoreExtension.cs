namespace DataServices.Core;

using System.Security.Cryptography;
using Azure.Core;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public static class DataServicesCoreExtension
{
    public static IHostBuilder AddDataServicesHandler(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddDbContext<DataServicesContext>(
                options => options.UseSqlServer(Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"))

            );
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IRequestHandler<>),typeof(RequestHandler<>)));
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IDataServiceAccessor<>),typeof(DataServiceAccessor<>)));

        });
    }

}
