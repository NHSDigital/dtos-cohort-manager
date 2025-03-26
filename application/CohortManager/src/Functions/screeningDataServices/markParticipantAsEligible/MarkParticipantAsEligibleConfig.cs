namespace NHS.Screening.MarkParticipantAsEligible;

using System.ComponentModel.DataAnnotations;

public class MarkParticipantAsEligibleConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
}
