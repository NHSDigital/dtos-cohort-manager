using Common;
using Common.Interfaces;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.StaticValidation;

var host = new HostBuilder()
    .AddConfiguration<StaticValidationConfig>(out StaticValidationConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IReadRules, ReadRules>();
        // Register health checks
        services.AddBasicHealthCheck("StaticValidation");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
