namespace NHS.Screening.RetrieveParticipantData;

using System.ComponentModel.DataAnnotations;

public class RetrieveParticipantDataConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string DemographicDataFunctionURL {get; set;}
}
