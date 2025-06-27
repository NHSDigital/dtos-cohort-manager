namespace NHS.CohortManager.CohortDistributionServices;

using System.ComponentModel.DataAnnotations;

public class DistributeParticipantConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }
    [Required]
    public string CohortQueueName { get; set; }
    [Required]
    public bool IgnoreParticipantExceptions { get; set; }
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
    public string DemographicDataFunctionURL { get; set; }
    public bool IsExtractedToBSSelect { get; set; } = false;
}