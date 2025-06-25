namespace Common;

using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class TelemetryExtension
{
    public static IHostBuilder AddTelemetry(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(_ =>
        {
            _.AddApplicationInsightsTelemetryWorkerService();
            _.ConfigureFunctionsApplicationInsights();
            _.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                {
                    module.SetComponentCorrelationHttpHeaders = true;
                });
            _.AddApplicationInsightsTelemetryProcessor<HealthCheckFilterTelemetryProcessor>();
        });
    }

}
