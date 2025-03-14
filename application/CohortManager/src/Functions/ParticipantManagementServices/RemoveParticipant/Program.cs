using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
        services.AddHttpClient<ICheckDemographic, CheckDemographic>(client =>
        {
            client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("DemographicURIGet"));
        });
        // Register health checks
        services.AddBasicHealthCheck("RemoveParticipant");
    })
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
