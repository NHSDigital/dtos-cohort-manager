namespace NHS.Screening.BlockParticipant;

using System.ComponentModel.DataAnnotations;

public class BlockParticipantConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string ParticipantDemographicDataServiceURL {get; set;}
}