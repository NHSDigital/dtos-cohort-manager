namespace Common;

using Azure.Identity;
using Hl7.FhirPath.Expressions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class AzureQueueExtension
{
    /// <summary>
    /// Extension method for adding service bus clients, if it detects a connection string, it will use connection
    /// string based auth, otherwise it will try and use managed identities
    /// </summary>
    public static IHostBuilder AddAzureQueues(this IHostBuilder hostBuilder, string serviceBusConnectionString)
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
                        .WithCredential(new DefaultAzureCredential());
                }
            });
            _.AddSingleton<IQueueClient, AzureServiceBusClient>();
        });

        return hostBuilder;
    }

    /// <summary>
    /// Extension method for adding service bus clients
    /// This will implement the queue client as a keyed service allowing it to be used in parallel with other queue types
    /// </summary>
    public static IHostBuilder AddKeyedAzureQueues(this IHostBuilder hostBuilder, string serviceBusConnectionString, string keyName)
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
                            .WithCredential(new DefaultAzureCredential());
                    }
                });
                _.AddKeyedSingleton<IQueueClient, AzureServiceBusClient>(keyName);
            });

        return hostBuilder;
    }


}
