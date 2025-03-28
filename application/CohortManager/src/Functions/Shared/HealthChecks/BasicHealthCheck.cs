namespace HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class BasicHealthCheck: IHealthCheck
{
    private readonly ILogger<BasicHealthCheck> _logger;
    public BasicHealthCheck(ILogger<BasicHealthCheck> logger)
    {
        _logger = logger;
    }
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running basic health check...");

        try
        {
            return await Task.FromResult(HealthCheckResult.Healthy("The service is up and running fine."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basic health check failed.");
            return await Task.FromResult(HealthCheckResult.Unhealthy("The service is down.", ex));
        }
    }
}
