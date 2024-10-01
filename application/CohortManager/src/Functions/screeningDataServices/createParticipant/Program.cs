using System.Data;
using System.Data.Common;
using Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
        var databaseConnectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString");
        services.AddTransient<IDbConnection>(provider =>
        {
            var providerFactory = DbProviderFactories.GetFactory("Microsoft.Data.SqlClient");
            var conn = providerFactory.CreateConnection();
            conn.ConnectionString = databaseConnectionString;
            return conn;
        });
        services.AddSingleton<ICreateParticipantData, CreateParticipantData>();
        services.AddSingleton<IParticipantManagerData, ParticipantManagerData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICallFunction, CallFunction>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();
