namespace Common;

using Azure.Identity;
using Hl7.FhirPath.Expressions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
    /// <summary>
    /// Extension method for adding service bus clients. Works with connection
    /// string or managed identies based auth
    /// </summary>
    public static IHostBuilder AddServiceBusClient(this IHostBuilder hostBuilder, string serviceBusConnectionString)
    {


        hostBuilder.ConfigureServices(_ =>
            {
            _.AddAzureClients(builder =>
            {
                if (serviceBusConnectionString.StartsWith("Endpoint="))
                {
                    builder.AddServiceBusClient(serviceBusConnectionString);
                }
                else
                {
                    builder.AddServiceBusClientWithNamespace(serviceBusConnectionString)
                        .WithCredential(new ManagedIdentityCredential ());
                }
            });
            _.AddSingleton<IQueueClient, AzureServiceBusClient>();
        });

        return hostBuilder;
    }

    /// <summary>
    /// Extension method that creates storage queue clients
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
                        if (serviceBusConnectionString.StartsWith("Endpoint="))
                        {
                            builder.AddServiceBusClient(serviceBusConnectionString);
                        }
                        else
                        {
                            builder.AddServiceBusClientWithNamespace(serviceBusConnectionString)
                                .WithCredential(new ManagedIdentityCredential ());
                        }
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
