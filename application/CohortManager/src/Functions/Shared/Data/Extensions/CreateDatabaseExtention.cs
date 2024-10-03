namespace Data.Database;

using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Microsoft.Extensions.Hosting;
using System.Data;
using Microsoft.Data.SqlClient;

public static class CreateDatabaseExtention
{

    public static IHostBuilder AddDatabaseConnection(this IHostBuilder hostBuilder)
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<IDbConnection>(provider =>
            {
                var providerFactory = DbProviderFactories.GetFactory("Microsoft.Data.SqlClient");
                var conn = providerFactory.CreateConnection();
                return conn;
            });
        });
    }
}

