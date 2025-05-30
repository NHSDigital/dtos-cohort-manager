namespace NHS.CohortManager.CohortDistributionService;

using System.ComponentModel.DataAnnotations;

public class CreateCohortDistributionConfig
{
    [Required]
    public bool IgnoreParticipantExceptions {get; set;}
    [Required]
    public string CohortQueueNamePoison {get; set;} 
    [Required]
    public string AddCohortDistributionURL {get; set;}
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
