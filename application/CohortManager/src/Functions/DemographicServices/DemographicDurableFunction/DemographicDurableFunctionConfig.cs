namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

public class DemographicDurableFunctionConfig
{
    [Required]
    public string DemographicDataServiceURL { get; set; }

    [Required]
    public int MaxRetryCount { get; set; }
}
