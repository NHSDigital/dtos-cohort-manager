using Microsoft.Extensions.Hosting;
using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddDatabaseHealthCheck("ExcludedSMULookupDataServices");
    })
    .Build();

await host.RunAsync();
