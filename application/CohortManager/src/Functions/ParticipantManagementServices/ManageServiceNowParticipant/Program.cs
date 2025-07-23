using Common;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using NHS.CohortManager.ParticipantManagementServices;

var host = new HostBuilder()
    .AddConfiguration(out ManageServiceNowParticipantConfig config)
        .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementURL)
        .Build()
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
