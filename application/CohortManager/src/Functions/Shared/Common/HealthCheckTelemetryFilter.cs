namespace Common;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

public class HealthCheckFilterTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public HealthCheckFilterTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request &&
            request.Url.AbsolutePath.Contains("/health", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (item is DependencyTelemetry dependency &&
            dependency.Data?.Contains("/health") == true)
        {
            return;
        }

        if (item is TraceTelemetry trace &&
            trace.Properties.TryGetValue("CategoryName", out var categoryName) &&
            categoryName.Contains("Health", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _next.Process(item);
    }
}
