using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.Screening.DemographicDurableFunction;

var host = new HostBuilder()
    .AddConfiguration<DurableAddFunctionConfig>(out DurableAddFunctionConfig config)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IAzureQueueStorageHelper, AzureQueueStorageHelper>();
        services.AddSingleton<IQueueClientFactory, QueueClientFactory>();
        // Register health checks
    })
    .AddAzureQueues()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
