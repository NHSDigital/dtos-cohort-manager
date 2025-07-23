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
    public required string ClientId { get; set; }

    [Required]
    public required string KId { get; set; }

    [Required]
    public required string AuthTokenURL { get; set; }

    [Required]
    public required string PrivateKey { get; set; }

    public required string privateKeyFileName { get; set; }
}
