namespace Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureServiceBussExtension
{

    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder, bool UseNewFunctions, string serviceBusConnectionString)
    {
        return hostBuilder.ConfigureServices(_ =>
        {

            _.AddScoped<IQueueClient>(_ => new AzureServiceBusClient(serviceBusConnectionString));

        });
    }
}