namespace NHS.Screening.DemographicDataManagementFunction;

using System.ComponentModel.DataAnnotations;

public class DemographicDataManagementFunctionConfig
{
    [Required]
    public string ParticipantDemographicDataServiceURL {get; set;}
}
