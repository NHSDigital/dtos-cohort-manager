using System.Data;
using System.Data.Common;
using Common;
using Data.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
        services.AddSingleton<IDbConnection>(provider =>
        {
            var providerFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            var conn = providerFactory.CreateConnection();
            return conn;
        });
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICohortDistributionHelper, CohortDistributionHelper>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<IParticipantManagerData, ParticipantManagerData>();
    })
    .AddExceptionHandler()
    .Build();

host.Run();
