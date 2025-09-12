namespace NHS.CohortManager.CohortDistributionServices;

using System.ComponentModel.DataAnnotations;

public class DistributeParticipantConfig
{
    [Required]
    public required string CohortDistributionTopic { get; set; }
    [Required]
    public required string DistributeParticipantSubscription { get; set; }
    [Required]
    public required string LookupValidationURL { get; set; }
    [Required]
    public required string StaticValidationURL { get; set; }
    [Required]
    public required string TransformDataServiceURL { get; set; }
    [Required]
    public required string ParticipantManagementUrl { get; set; }
    [Required]
    public required string CohortDistributionDataServiceUrl { get; set; }
    [Required]
    public required string ParticipantDemographicDataServiceUrl { get; set; }
    [Required]
    public required string RemoveOldValidationRecordUrl { get; set; }
    public required string SendServiceNowMessageURL { get; set; }
    public int MaxLookupValidationRetries { get; set; } = 3;
    public bool IsExtractedToBSSelect { get; set; } = false;
    public bool IgnoreParticipantExceptions { get; set; } = false;
}
