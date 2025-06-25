namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

public class RetrievePDSDemographicConfig
{
    [Required]
    public required string RetrievePdsParticipantURL { get; set; }

    [Required]
    public required string DemographicDataServiceURL { get; set; }
}
