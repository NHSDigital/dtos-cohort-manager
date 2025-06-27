using Common;
using DataServices.Core;
using DataServices.Database;
using HealthChecks.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddDatabaseHealthCheck("ReconciliationService");
        services.AddSingleton<ICreateResponse, CreateResponse>();
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
