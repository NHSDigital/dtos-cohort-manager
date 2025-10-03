namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageParticipantConfig
{
    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public required string CohortDistributionTopic { get; set; }
    [Required]
    public required string ParticipantManagementTopic { get; set; }
    [Required]
    public required string ManageParticipantSubscription { get; set; }
    [Required]
    public required string ParticipantManagementUrl { get; set; }
}