namespace DataServices.Core;

using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public static class DataServicesCoreExtension
{
    public static IHostBuilder AddDataServicesHandler<DBContextType>(this IHostBuilder hostBuilder, string? connectionString = null, Action<DbContextOptionsBuilder> dbContextOptionsBuilder = null) where DBContextType : DbContext
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            if(dbContextOptionsBuilder == null)
            {
                _.AddDbContext<DBContextType>(
                    options =>  options.UseSqlServer(connectionString ?? Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"))

                );
            }
            else
            {
                _.AddDbContext<DBContextType>(dbContextOptionsBuilder);
            }
            _.AddSingleton<ICreateResponse, CreateResponse>();
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IRequestHandler<>), typeof(RequestHandler<>)));
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IDataServiceAccessor<>), typeof(DataServiceAccessor<>)));

        });
    }

}
