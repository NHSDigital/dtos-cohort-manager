namespace NHS.Screening.RemoveParticipant;

using System.ComponentModel.DataAnnotations;

public class RemoveParticipantConfig
{
    [Required]
    public required string UpdateParticipant {get; set;}
}
