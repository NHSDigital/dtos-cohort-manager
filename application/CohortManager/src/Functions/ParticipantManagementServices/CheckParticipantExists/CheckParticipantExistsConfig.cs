namespace NHS.Screening.CheckParticipantExists;

using System.ComponentModel.DataAnnotations;

public class CheckParticipantExistsConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
}
