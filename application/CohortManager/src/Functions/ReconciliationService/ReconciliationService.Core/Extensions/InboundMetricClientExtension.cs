namespace NHS.CohortManager.ReconciliationServiceCore;

using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class InboundMetricClientExtension
{
    public static IHostBuilder AddInboundMetricTracker(this IHostBuilder hostBuilder)
    {
        hostBuilder.AddConfiguration<InboundMetricClientConfig>(out InboundMetricClientConfig config);
        hostBuilder.AddKeyedAzureQueues(true, config.ServiceBusConnectionString_client_internal, "InboundMetricQueue");
        hostBuilder.ConfigureServices(_ =>
        {
            _.AddSingleton<IInboundMetricClient, InboundMetricClient>();
        });
        return hostBuilder;
    }
}
