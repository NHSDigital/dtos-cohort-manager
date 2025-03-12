namespace NHS.Screening.UpdateParticipantDetails;

using System.ComponentModel.DataAnnotations;

public class UpdateParticipantDetailsConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string LookupValidationURL {get; set;}
}
