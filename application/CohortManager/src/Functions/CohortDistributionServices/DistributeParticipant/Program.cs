using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.CohortDistributionServices;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;

var host = new HostBuilder()
    .AddConfiguration<DistributeParticipantConfig>(out DistributeParticipantConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddBasicHealthCheck("DistributeParticipant");
    })
    .AddAzureQueues(true, config.ServiceBusConnectionString)
    .AddExceptionHandler()
    .AddTelemetry()
    .Build();


await host.RunAsync();