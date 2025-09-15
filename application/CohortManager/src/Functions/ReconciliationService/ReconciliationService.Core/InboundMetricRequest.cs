namespace NHS.CohortManager.ReconciliationServiceCore;

public class InboundMetricRequest
{
    public required string AuditProcess { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public required string Source { get; set; }
    public int RecordCount { get; set; }
}
