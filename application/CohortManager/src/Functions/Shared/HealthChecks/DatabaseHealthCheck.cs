namespace HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class DatabaseHealthCheck<TDbContext> : IHealthCheck
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck<TDbContext>> _logger;

    public DatabaseHealthCheck(ILogger<DatabaseHealthCheck<TDbContext>> logger, TDbContext dbContext)
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
            bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database is healthy.")
                : HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
