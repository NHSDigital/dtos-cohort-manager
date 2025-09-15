namespace NHS.CohortManager.ReconciliationServiceCore;

using System.ComponentModel.DataAnnotations;

public class InboundMetricClientConfig
{
    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public required string InboundMetricTopic { get; set; }
}
