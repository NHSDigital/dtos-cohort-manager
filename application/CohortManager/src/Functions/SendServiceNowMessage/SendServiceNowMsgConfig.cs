namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class AddParticipantConfig
{
    [Required]
    public string UpdateEndpoint {get; set;}
    [Required]
    public string AccessToken {get; set;}
}
