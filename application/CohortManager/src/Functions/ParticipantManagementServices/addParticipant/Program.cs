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
        services.AddHttpClient<ICheckDemographic, CheckDemographic>(client =>
        {
            client.BaseAddress = new Uri(config.DemographicURIGet);
        });
        // Register health checks
        services.AddBlobStorageHealthCheck("addParticipant");
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
