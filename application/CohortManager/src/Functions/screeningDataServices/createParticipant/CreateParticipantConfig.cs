namespace NHS.Screening.CreateParticipant;

using System.ComponentModel.DataAnnotations;

public class CreateParticipantConfig
{
    [Required]
    public string ParticipantManagementUrl { get; set; }
    [Required]
    public string LookupValidationURL {get; set;}
}
