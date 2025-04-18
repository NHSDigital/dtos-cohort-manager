namespace NHS.Screening.MarkParticipantAsIneligible;

using System.ComponentModel.DataAnnotations;

public class MarkParticipantAsIneligibleConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string LookupValidationURL { get; set; }
}
