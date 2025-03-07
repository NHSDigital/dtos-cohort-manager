namespace NHS.Screening.ValidateCohortDistributionRecord;

using System.ComponentModel.DataAnnotations;

public class ValidateCohortDistributionRecordConfig
{
    [Required]
    public string CohortDistributionDataServiceURL {get; set;}
}
