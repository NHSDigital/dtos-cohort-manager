namespace NHS.Screening.RemoveParticipant;

using System.ComponentModel.DataAnnotations;

public class RemoveParticipantConfig
{
    [Required]
    public string DemographicURIGet {get; set;}
    [Required]
    public string markParticipantAsIneligible {get; set;}
}
