namespace ReconciliationServiceCore;

using System.ComponentModel.DataAnnotations;

public class InboundMetricClientConfig
{
    [Required]
    public required string ServiceBusConnectionString { get; set; }
    [Required]
    public required string InboundMetricTopic { get; set; }
}
