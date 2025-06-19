namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class SendServiceNowMsgConfig
{
    [Required]
    public required string UpdateEndpoint { get; set; }
    [Required]
    public required string AccessToken { get; set; }
    [Required]
    public required string ServiceNowBaseUrl { get; set; }
    [Required]
    public required string Profile { get; set; }
    [Required]
    public required string Definition { get; set; }
    [Required]
    public required string EndpointPath { get; set; }
}

