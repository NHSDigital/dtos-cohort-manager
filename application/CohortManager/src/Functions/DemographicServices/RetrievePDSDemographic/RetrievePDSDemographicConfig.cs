namespace NHS.Screening.RetrievePDSDemographic;

using System.ComponentModel.DataAnnotations;

public class RetrievePDSDemographicConfig
{
    [Required]
    public string RetrievePdsParticipantURL { get; set; }

    [Required]
    public string DemographicDataServiceURL { get; set; }
}
