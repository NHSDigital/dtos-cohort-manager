namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

public class DemographicDurableFunctionConfig
{
    [Required]
    public required string DemographicDataServiceURL {get; set;}
    [Required]
    public required int MaxRetryCount { get; set; }

}
