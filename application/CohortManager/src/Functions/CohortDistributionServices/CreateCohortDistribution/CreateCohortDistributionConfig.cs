namespace NHS.Screening.CreateCohortDistribution;

using System.ComponentModel.DataAnnotations;

public class CreateCohortDistributionConfig
{
    [Required]
    public bool IgnoreParticipantExceptions { get; set; }
    [Required]
    public string CohortQueueNamePoison { get; set; }
    [Required]
    public string AddCohortDistributionURL { get; set; }

    public string LookupValidationURL { get; set; }
}
