namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IQueueClient>(_ => new AzureServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString") ?? ""));

            _.AddTransient<IQueueClient, AzureStorageQueueClient>();
            _.AddTransient<IQueueClientFactory, QueueClientFactory>();
        });
    }

}
