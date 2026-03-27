using Common;
using Common.Interfaces;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRules, ReadRules>();
        services.AddSingleton<IReasonForRemovalLookup,ReasonForRemovalLookup>();
        // Register health checks
        services.AddBasicHealthCheck("StaticValidation");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
