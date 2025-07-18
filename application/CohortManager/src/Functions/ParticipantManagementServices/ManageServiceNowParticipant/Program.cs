using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ParticipantManagementServices;

var host = new HostBuilder()
    .AddConfiguration(out ManageServiceNowParticipantConfig _)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddBasicHealthCheck("ManageServiceNowParticipant");
    })
    .AddTelemetry()
    .AddAzureQueues()
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
