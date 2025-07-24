namespace ReconciliationServiceCore;

public interface IInboundMetricClient
{
    Task<bool> LogInboundMetric(string source, int recordCount);
}
