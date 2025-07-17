namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public string CohortDistributionTopic { get; set; }
    [Required]
    public string ParticipantManagementTopic { get; set; }
    [Required]
    public string ManageParticipantSubscription { get; set; }
    [Required]
    public string ParticipantManagementUrl { get; set; }
}