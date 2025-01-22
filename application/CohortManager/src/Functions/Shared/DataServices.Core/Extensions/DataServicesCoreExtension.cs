namespace DataServices.Core;

using Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public static class DataServicesCoreExtension
{

    public static IHostBuilder AddDataServicesHandler<DBContextType>(this IHostBuilder hostBuilder, AuthenticationConfiguration authenticationConfiguration) where DBContextType : DbContext
    {
        return AddDataServicesHandler<DBContextType>(hostBuilder, null, null, authenticationConfiguration);
    }

    public static IHostBuilder AddDataServicesHandler<DBContextType>(
        this IHostBuilder hostBuilder,
        string? connectionString = null,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder = null,
        AuthenticationConfiguration authenticationConfiguration = null

    ) where DBContextType : DbContext
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            if (dbContextOptionsBuilder == null)
            {
                _.AddDbContext<DBContextType>(
                    options => options.UseSqlServer(connectionString ?? Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString"), sqlServerOptions => sqlServerOptions.CommandTimeout(180))
                );
            }
            else
            {
                _.AddDbContext<DBContextType>(dbContextOptionsBuilder);
            }

            if (authenticationConfiguration == null)
            {
                AccessRule alwaysTrueRule = i => true;
                authenticationConfiguration = new AuthenticationConfiguration(alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule);
            }

            _.AddSingleton(authenticationConfiguration);
            _.AddSingleton<ICreateResponse, CreateResponse>();
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IRequestHandler<>), typeof(RequestHandler<>)));
            _.TryAdd(ServiceDescriptor.Scoped(typeof(IDataServiceAccessor<>), typeof(DataServiceAccessor<>)));

        });
    }

}
