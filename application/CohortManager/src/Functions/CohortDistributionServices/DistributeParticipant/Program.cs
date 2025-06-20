using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.CohortDistributionServices;
// using DataServices.Client;
// using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<DistributeParticipantConfig>(out DistributeParticipantConfig config)
    .ConfigureFunctionsWebApplication()
    // .ConfigureServices(services => {
    //     // Register health checks
    //     services.AddBasicHealthCheck("CheckParticipantExists");
    // })
    .AddAzureQueues()
    .Build();


await host.RunAsync();