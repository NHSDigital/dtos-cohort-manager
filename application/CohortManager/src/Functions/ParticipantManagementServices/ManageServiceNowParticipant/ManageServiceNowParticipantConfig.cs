namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageServiceNowParticipantConfig
{
    [Required, Url]
    public required string RetrievePdsDemographicURL { get; set; }
    [Required, Url]
    public required string SendServiceNowMessageURL { get; set; }
}
