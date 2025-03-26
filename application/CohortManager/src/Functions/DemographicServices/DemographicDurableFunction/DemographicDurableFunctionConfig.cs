namespace NHS.Screening.DemographicDurableFunction;

using System.ComponentModel.DataAnnotations;

public class DemographicDurableFunctionConfig
{
    [Required]
    public string DemographicDataServiceURL {get; set;}
}
