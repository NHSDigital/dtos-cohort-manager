namespace Common;

using Azure.Identity;
using Hl7.FhirPath.Expressions;
using Microsoft.Extensions.Azure;
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


        hostBuilder.ConfigureServices(_ =>
            {
            if (UseNewFunctions)
            {
                _.AddAzureClients(builder =>
                {
                    builder.AddServiceBusClient(serviceBusConnectionString);
                    builder.UseCredential(new DefaultAzureCredential());

                });
                _.AddSingleton<IQueueClient, AzureServiceBusClient>();
            }
            else
            {
                _.AddTransient<IQueueClient, AzureStorageQueueClient>();
                _.AddTransient<IQueueClientFactory, QueueClientFactory>();
            }
        });

        return hostBuilder;
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
     /// <summary>
    /// Extension method for adding azure queue clients, if UseNewFunctions is set to true, it will inject a service bus queue client,
    /// otherwise, it will inject a azure storage queue client
    /// This will implement the queue client as a keyed service allowing it to be used in parallel with other queue types
    /// </summary>
    public static IHostBuilder AddKeyedAzureQueues(this IHostBuilder hostBuilder, bool UseNewFunctions, string serviceBusConnectionString, string keyName)
    {


        hostBuilder.ConfigureServices(_ =>
            {
                if (UseNewFunctions)
                {
                    _.AddAzureClients(builder =>
                    {
                        builder.AddServiceBusClient(serviceBusConnectionString)
                            .WithCredential(new DefaultAzureCredential());

                    });
                    _.AddKeyedSingleton<IQueueClient, AzureServiceBusClient>(keyName);
                }
                else
                {
                    _.AddKeyedTransient<IQueueClient, AzureStorageQueueClient>(keyName);
                    _.AddTransient<IQueueClientFactory, QueueClientFactory>();
                }
            });

        return hostBuilder;
    }


}
