using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.UpdateParticipant;

var host = new HostBuilder()
    .AddConfiguration<UpdateParticipantConfig>(out UpdateParticipantConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        // Register health checks
        services.AddBasicHealthCheck("updateParticipant");
    })
    .AddAzureQueues(false, "")
    .AddExceptionHandler()
    .AddHttpClient()
    .Build();

await host.RunAsync();
