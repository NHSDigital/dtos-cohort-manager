using System.Data;
using System.Data.Common;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        services.AddTransient<IDbConnection>(provider =>
        {
            var providerFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            var conn = providerFactory.CreateConnection();
            return conn;
        });
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateCohortDistributionData, CreateCohortDistributionData>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();