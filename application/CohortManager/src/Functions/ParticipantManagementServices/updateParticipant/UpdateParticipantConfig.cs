namespace NHS.Screening.UpdateParticipant;

using System.ComponentModel.DataAnnotations;

public class UpdateParticipantConfig
{
    [Required]
    public string DemographicURIGet { get; set; }
    [Required]
    public string UpdateParticipant { get; set; }
    [Required]
    public string StaticValidationURL { get; set; }
    [Required]
    public string DSmarkParticipantAsEligible { get; set; }
    [Required]
    public string markParticipantAsIneligible { get; set; }
}
