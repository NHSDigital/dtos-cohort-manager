namespace NHS.Screening.ProcessNemsUpdate;

using System.ComponentModel.DataAnnotations;

public class ProcessNemsUpdateConfig
{
    public required string RetrievePdsDemographicURL { get; set; }
    public required string NemsMessages { get; set; }
    public required string UnsubscribeNemsSubscriptionUrl { get; set; }

    public string ServiceBusConnectionString_client_internal { get; set; } = null!;

    public string ParticipantManagementTopic { get; set; } = null!;

}
