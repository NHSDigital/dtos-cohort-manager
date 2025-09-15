namespace NHS.CohortManager.ReconciliationServiceCore;

public interface IInboundMetricClient
{
    /// <summary>
    /// Captures and sends a metric for reconciliation to the Inbound Metrics Processor
    /// This will be logged to the Inbound_Metric Database Table
    /// </summary>
    /// <param name="source"></param>
    /// <param name="recordCount"></param>
    /// <returns>Returns a boolean that states if logging the metric was successful</returns>
    Task<bool> LogInboundMetric(string source, int recordCount);
}
