namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class ManageServiceNowParticipantConfig
{
    [Required, Url]
    public required string RetrievePdsDemographicURL { get; set; }
    [Required, Url]
    public required string SendServiceNowMessageURL { get; set; }
    [Required, Url]
    public required string ParticipantManagementURL { get; set; }
    [Required, Url]
    public required string ManageNemsSubscriptionSubscribeURL { get; set; }
    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public required string CohortDistributionTopic { get; set; }
}
