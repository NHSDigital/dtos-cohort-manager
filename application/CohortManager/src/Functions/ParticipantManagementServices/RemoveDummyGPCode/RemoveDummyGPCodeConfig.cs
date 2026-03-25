namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;

public class RemoveDummyGpCodeConfig
{
    [Required, Url]
    public required string RetrievePdsDemographicURL { get; set; }

    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }

    [Required]
    public required string ServiceNowParticipantManagementTopic { get; set; }
}
