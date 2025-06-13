namespace NHS.CohortManager.ParticipantManagementService;

using System.ComponentModel.DataAnnotations;

public class UpdateBlockedFlagConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string ParticipantDemographicDataServiceURL {get; set;}
    [Required]
    public string ExceptionFunctionURL {get; set;}
}