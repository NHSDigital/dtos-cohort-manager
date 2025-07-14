namespace NHS.Screening.AddParticipant;

using System.ComponentModel.DataAnnotations;

public class AddParticipantConfig
{
    [Required]
    public string DemographicURIGet {get; set;}
    [Required]
    public string DSaddParticipant {get; set;}
    [Required]
    public string StaticValidationURL {get; set;}
}
