namespace NHS.CohortManager.ReconciliationServiceCore;

public interface IInboundMetricClient
{
    Task<bool> LogInboundMetric(string source, int recordCount);
}
