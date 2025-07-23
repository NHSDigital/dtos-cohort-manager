namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageServiceNowParticipantConfig
{
    [Required, Url]
    public required string RetrievePdsDemographicUrl { get; set; }
    [Required, Url]
    public required string SendServiceNowMessageUrl { get; set; }
    [Required, Url]
    public required string ParticipantManagementUrl { get; set; }
}
