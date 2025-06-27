namespace ReconciliationServiceCore;

using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class InboundMetricClientExtension
{
    public static IHostBuilder AddInboundMetricTracker(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddConfiguration<InboundMetricClientConfig>(out InboundMetricClientConfig config);
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IQueueClient>(_ => new AzureServiceBusClient(config.ServiceBusConnectionString));
            _.AddSingleton<IInboundMetricClient, InboundMetricClient>();
        });
    }
}
