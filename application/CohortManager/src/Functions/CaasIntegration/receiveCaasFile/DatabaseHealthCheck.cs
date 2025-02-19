using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using DataServices.Client;
using Model;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDataServiceClient<ScreeningLkp> _screeningLkpClient;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IDataServiceClient<ScreeningLkp> screeningLkpClient, ILogger<DatabaseHealthCheck> logger)
    {
        _screeningLkpClient = screeningLkpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a simple query to check database connectivity
            var result = await _screeningLkpClient.GetSingleByFilter(x => x.ScreeningWorkflowId == "CAAS_BREAST_SCREENING_COHORT");
            return HealthCheckResult.Healthy("Database is accessible.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed.");
            return HealthCheckResult.Unhealthy("Database is inaccessible.", ex);
        }
    }
}
