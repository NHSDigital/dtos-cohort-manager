namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }
    [Required]
    public string CohortQueueName { get; set; }
    [Required]
    public string ParticipantManagementQueueName { get; set; }
    [Required]
    public string ParticipantManagementUrl { get; set; }
}