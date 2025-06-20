using Microsoft.Extensions.Hosting;
using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;
using Common;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddDatabaseHealthCheck("BsSelectRequestAuditDataService");
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
