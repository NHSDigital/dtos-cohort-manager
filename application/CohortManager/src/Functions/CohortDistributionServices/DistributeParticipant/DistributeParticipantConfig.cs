namespace NHS.CohortManager.CohortDistributionServices;

using System.ComponentModel.DataAnnotations;

public class DistributeParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }
    [Required]
    public string CohortQueueName { get; set; }
}