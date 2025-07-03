namespace NHS.Screening.RemoveParticipant;

using System.ComponentModel.DataAnnotations;

public class RemoveParticipantConfig
{
    [Required]
    public required string UpdateParticipant { get; set; }
    [Required]
    public required string ExceptionFunctionURL { get; set; }
    [Required]
    public required string ParticipantManagementUrl {get; set;}
}
