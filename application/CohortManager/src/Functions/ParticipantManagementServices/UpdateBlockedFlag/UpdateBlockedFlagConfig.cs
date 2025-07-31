namespace NHS.CohortManager.ParticipantManagementService;

using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Microsoft.Azure.Amqp;

public class UpdateBlockedFlagConfig
{
    [Required]
    public required string ParticipantManagementUrl { get; set; }
    [Required]
    public required string ParticipantDemographicDataServiceURL { get; set; }
    [Required]
    public required string ExceptionFunctionURL { get; set; }
    [Required]
    public required string ManageNemsSubscriptionUnsubscribeURL { get; set; }
    [Required]
    public required string ManageNemsSubscriptionSubscribeURL { get; set; }
    [Required]
    public required string RetrievePdsDemographicURL { get; set; }
}
