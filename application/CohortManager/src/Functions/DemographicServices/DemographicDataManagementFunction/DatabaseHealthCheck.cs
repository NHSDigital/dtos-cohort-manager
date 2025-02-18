using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using DataServices.Client;
using Model;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    public DatabaseHealthCheck(IDataServiceClient<ParticipantDemographic> participantDemographic)
    {
        _participantDemographic = participantDemographic;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a test query to ensure the DB is reachable
            var testResult = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber != 0);
            return testResult != null
                ? HealthCheckResult.Healthy("Database is reachable")
                : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database connection failed");
        }
    }
}
