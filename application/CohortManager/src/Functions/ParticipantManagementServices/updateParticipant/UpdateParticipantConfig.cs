namespace NHS.Screening.UpdateParticipant;

using System.ComponentModel.DataAnnotations;

public class UpdateParticipantConfig
{
    [Required]
    public string DemographicURIGet {get; set;}
}
