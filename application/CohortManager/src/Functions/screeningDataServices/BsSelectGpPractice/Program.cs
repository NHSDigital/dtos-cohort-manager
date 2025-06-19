using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
        services.AddDatabaseHealthCheck("BsSelectGpPractice");
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
