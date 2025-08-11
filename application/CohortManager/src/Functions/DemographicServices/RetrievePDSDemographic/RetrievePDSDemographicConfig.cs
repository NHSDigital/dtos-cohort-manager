namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

public class RetrievePDSDemographicConfig
{
    [Required]
    public required string RetrievePdsParticipantURL { get; set; }

    [Required]
    public required string DemographicDataServiceURL { get; set; }

    [Required]
    public required string Audience { get; set; }

    [Required]
    public required string KId { get; set; }

    [Required]
    public required string AuthTokenURL { get; set; }

    [Required]
    public required string ParticipantManagementTopic { get; set; }

    public string ServiceBusConnectionString_client_internal { get; set; }

    public required bool UseFakePDSServices { get; set; } = false;

    public string ClientId { get; set; } = string.Empty;

    [ConfigurationKeyName("ServiceBusConnectionString_client_internal")]
    public string? ServiceBusConnectionStringClientInternal { get; set; }

    [Required]
    public string EffectiveServiceBusConnectionString
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ServiceBusConnectionString))
            {
                return ServiceBusConnectionString;
            }

            if (!string.IsNullOrWhiteSpace(ServiceBusConnectionStringClientInternal))
            {
                return ServiceBusConnectionStringClientInternal;
            }

            throw new InvalidOperationException(
                "Missing Service Bus connection string. " +
                "Set ServiceBusConnectionString or ServiceBusConnectionString_client_internal.");
        }
    }
}
