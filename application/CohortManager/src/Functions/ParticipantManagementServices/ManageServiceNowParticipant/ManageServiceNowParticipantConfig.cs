namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageServiceNowParticipantConfig
{
    [Required]
    public required string RetrievePdsDemographicURL { get; set; }
    [Required]
    public required string SendServiceNowMessageURL { get; set; }
}
