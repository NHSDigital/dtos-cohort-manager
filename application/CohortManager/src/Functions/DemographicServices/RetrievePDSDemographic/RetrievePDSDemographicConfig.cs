namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

public class RetrievePDSDemographicConfig
{

    [Required]
    public required string RetrievePdsParticipantURL { get; set; }

    [Required]
    public required string DemographicDataServiceURL { get; set; }

    [Required]
    public required string Audience { get; set; }

    [Required]
    public required string KId { get; set; }

    [Required]
    public required string AuthTokenURL { get; set; }

    public required bool UseFakePDSServices { get; set; } = false;
}
