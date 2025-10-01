namespace NHS.Screening.DemographicDurableFunction;

using System.ComponentModel.DataAnnotations;

public class DemographicDurableFunctionConfig
{
    [Required]
    public required string DemographicDataServiceURL {get; set;}
}
