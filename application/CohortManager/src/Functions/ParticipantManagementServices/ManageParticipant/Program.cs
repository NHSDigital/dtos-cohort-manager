using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.ParticipantManagementServices;

var host = new HostBuilder()
    .AddConfiguration<ManageParticipantConfig>(out ManageParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    .AddTelemetry()
    // .ConfigureServices(services => {
    //     // Register health checks
    //     services.AddBasicHealthCheck("CheckParticipantExists");
    // })
    .AddAzureQueues()
    .Build();


await host.RunAsync();