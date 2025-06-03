namespace NHS.Screening.RemoveParticipant;

using System.ComponentModel.DataAnnotations;

public class RemoveParticipantConfig
{
    [Required]
    public string UpdateParticipant {get; set;}
}
