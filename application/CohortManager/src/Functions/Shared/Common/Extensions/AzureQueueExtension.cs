namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
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

}
