using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("AllocateServiceProviderToParticipantByService");
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
