using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()

    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICallFunction, CallFunction>();
        services.AddSingleton<ICheckDemographic, CheckDemographic>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        services.AddSingleton<ICohortDistributionHandler, CohortDistributionHandler>();
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
        services.AddHttpClient<ICheckDemographic, CheckDemographic>(client =>
        {
            client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("DemographicURIGet"));
        });
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
