namespace HealthChecks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using DataServices.Database;
using Microsoft.Extensions.Logging;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DataServicesContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ILogger<DatabaseHealthCheck> logger, DataServicesContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running health check for Sql-Server database...");
        try
        {
            // Use CanConnectAsync to check if the database is reachable and running online
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            // Check latency of sql-server
            var isDatabaseLatencyAcceptable = await CheckDatabaseLatencyAsync();

            return canConnect switch
            {
                true when isDatabaseLatencyAcceptable => HealthCheckResult.Healthy("Database is healthy."),
                true when !isDatabaseLatencyAcceptable => HealthCheckResult.Degraded("Database is very slow."),
                _ => HealthCheckResult.Unhealthy("Database is down or inaccessible.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sql-Server Database health check failed.");
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }

    private async Task<bool> CheckDatabaseLatencyAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            var endTime = DateTime.UtcNow;

            // Calculate the latency
            var latency = endTime - startTime;
            // Fetch threshold for acceptable latency (e.g., 500ms) from Environment Variable
            var value = Environment.GetEnvironmentVariable("AcceptableLatencyThresholdMs");
            var acceptableLatencyThresholdMs = int.Parse(value ?? throw new InvalidOperationException("Environment variable AcceptableLatencyThresholdMs is required."));
            return latency.TotalMilliseconds < acceptableLatencyThresholdMs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occured whilst checking database latency");
            return false;
        }
    }
}
