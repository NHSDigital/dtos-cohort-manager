using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.AddParticipant;

var host = new HostBuilder()
    .AddConfiguration<AddParticipantConfig>(out AddParticipantConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        // Register health checks
        services.AddBlobStorageHealthCheck("addParticipant");
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
