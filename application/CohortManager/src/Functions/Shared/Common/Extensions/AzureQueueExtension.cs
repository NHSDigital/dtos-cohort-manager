namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
    /// <summary>
    /// Extension method for adding azure queue clients, if UseNewFunctions is set to true, it will inject a service bus queue client,
    /// otherwise, it will inject a azure storage queue client
    /// </summary>
    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder, bool UseNewFunctions, string serviceBusConnectionString)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            if (UseNewFunctions)
            {
                _.AddSingleton<IQueueClient>(_ => new AzureServiceBusClient(serviceBusConnectionString));
            }
            else
            {
                _.AddTransient<IQueueClient, AzureStorageQueueClient>();
                _.AddTransient<IQueueClientFactory, QueueClientFactory>();
            }
        });
    }

    /// <summary>
    /// Overload that creates storage queue clients for instances where only storage queues
    /// will be used and we do not need control what is injected via config
    /// </summary>
    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddTransient<IQueueClient, AzureStorageQueueClient>();
            _.AddTransient<IQueueClientFactory, QueueClientFactory>();
        });
    }
}
