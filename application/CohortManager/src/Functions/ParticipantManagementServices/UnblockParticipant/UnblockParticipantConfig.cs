namespace NHS.Screening.UnblockParticipant;

using System.ComponentModel.DataAnnotations;

public class UnblockParticipantConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string ParticipantDemographicDataServiceURL {get; set;}
    [Required]
    public string ExceptionFunctionURL {get; set;}
}