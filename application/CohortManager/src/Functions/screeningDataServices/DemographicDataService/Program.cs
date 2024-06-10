using System.Data;
using System.Data.Common;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using Microsoft.Data.SqlClient;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        var databaseCOnnectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString");
        services.AddTransient<IDbConnection>(provider =>
        {
            var providerFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            var conn = providerFactory.CreateConnection();
            conn.ConnectionString = databaseCOnnectionString;
            return conn;
        });
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateDemographicData, CreateDemographicData>();
    })
    .Build();

host.Run();
