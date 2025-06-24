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
    public string CohortQueueNamePoison { get; set; }
    [Required]
    public string LookupValidationURL { get; set; }
    [Required]
    public string TransformDataServiceURL { get; set; }
    [Required]
    public string AllocateScreeningProviderURL { get; set; }
    [Required]
    public string RetrieveParticipantDataURL { get; set; }
    [Required]
    public string ParticipantManagementUrl { get; set; }
    [Required]
    public string CohortDistributionDataServiceUrl { get; set; }
}