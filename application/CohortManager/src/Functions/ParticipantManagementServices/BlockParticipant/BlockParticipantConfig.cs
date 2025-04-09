namespace NHS.Screening.BlockParticipant;

using System.ComponentModel.DataAnnotations;

public class BlockParticipantConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
}