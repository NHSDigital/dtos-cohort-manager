using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Common;
using DataServices.Client;
using NHS.Screening.BlockParticipant;
using Model;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<BlockParticipantConfig>(out BlockParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler()
        .AddDataService<ParticipantManagement>(config.ParticipantManagementUrl)
        .AddDataService<ParticipantDemographic>(config.ParticipantDemographicDataServiceURL)
        .Build()
    .ConfigureServices(services => {
        // Register health checks
        services.AddBasicHealthCheck("CheckParticipantExists");
    })
    .Build();

await host.RunAsync();