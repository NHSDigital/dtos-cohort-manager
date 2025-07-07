using Microsoft.Extensions.Hosting;
using Common;
using Model;
using NHS.CohortManager.ParticipantManagementServices;
using DataServices.Client;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<ManageParticipantConfig>(out ManageParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .Build()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddBasicHealthCheck("ManageParticipant");
    })
    .AddExceptionHandler()
    .AddAzureQueues(true, config.ServiceBusConnectionString_internal)
    .AddTelemetry()
    .Build();


await host.RunAsync();