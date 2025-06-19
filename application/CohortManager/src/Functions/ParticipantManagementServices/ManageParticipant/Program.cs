using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.ParticipantManagementServices;
// using DataServices.Client;
// using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<ManageParticipantConfig>(out ManageParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    // .ConfigureServices(services => {
    //     // Register health checks
    //     services.AddBasicHealthCheck("CheckParticipantExists");
    // })
    .Build();

await host.RunAsync();