namespace NHS.Screening.AddCohortDistribution;

using System.ComponentModel.DataAnnotations;

public class AddCohortDistributionDataConfig
{
    [Required]
    public string CohortDistributionDataServiceURL {get; set;}
}
