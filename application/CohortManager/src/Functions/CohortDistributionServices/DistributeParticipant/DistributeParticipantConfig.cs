namespace NHS.CohortManager.CohortDistributionServices;

using System.ComponentModel.DataAnnotations;

public class DistributeParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString_internal { get; set; }
    [Required]
    public string CohortDistributionTopic { get; set; }
    [Required]
    public string DistributeParticipantSubscription { get; set; }
    public bool IgnoreParticipantExceptions { get; set; } = false;
    [Required]
    public string LookupValidationURL { get; set; }
    [Required]
    public string StaticValidationURL { get; set; }
    [Required]
    public string TransformDataServiceURL { get; set; }
    [Required]
    public string ParticipantManagementUrl { get; set; }
    [Required]
    public string CohortDistributionDataServiceUrl { get; set; }
    [Required]
    public string ParticipantDemographicDataServiceUrl { get; set; }
    public bool IsExtractedToBSSelect { get; set; } = false;
}