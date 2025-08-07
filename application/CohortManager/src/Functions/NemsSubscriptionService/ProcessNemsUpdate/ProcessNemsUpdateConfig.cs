namespace NHS.Screening.ProcessNemsUpdate;

using System.ComponentModel.DataAnnotations;

public class ProcessNemsUpdateConfig
{
    public required string RetrievePdsDemographicURL { get; set; }
    public required string NemsMessages { get; set; }
    public required string UnsubscribeNemsSubscriptionUrl { get; set; }
    public required string ParticipantDemographicDataServiceURL { get; set; }
    public required string ServiceBusConnectionString_client_internal { get; set; }
    public required string ParticipantManagementTopic { get; set; }
}
