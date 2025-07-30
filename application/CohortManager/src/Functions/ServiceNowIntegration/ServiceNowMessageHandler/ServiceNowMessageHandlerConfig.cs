namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class ServiceNowMessageHandlerConfig
{
    [Required]
    public required string ServiceNowRefreshAccessTokenUrl { get; set; }
    [Required]
    public required string ServiceNowUpdateUrl { get; set; }
    [Required]
    public required string ServiceNowClientId { get; set; }
    [Required]
    public required string ServiceNowClientSecret { get; set; }
    [Required]
    public required string ServiceNowRefreshToken { get; set; }
}
