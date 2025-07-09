using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.CohortDistributionServices;
using DataServices.Client;
using HealthChecks.Extensions;
using Model;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .AddConfiguration<DistributeParticipantConfig>(out DistributeParticipantConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceUrl)
        .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceUrl)
        .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Register health checks
        services.AddBasicHealthCheck("DistributeParticipant");
    })
    .AddExceptionHandler()
    .AddTelemetry()
    .Build();


await host.RunAsync();