namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }
}