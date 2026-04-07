using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;
using Common;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        services.AddDatabaseHealthCheck("AuditWriter");
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
