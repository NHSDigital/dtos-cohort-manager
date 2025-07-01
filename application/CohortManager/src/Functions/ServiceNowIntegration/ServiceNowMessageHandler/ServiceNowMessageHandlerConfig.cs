namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class ServiceNowMessageHandlerConfig
{
    [Required]
    public required string ServiceNowRefreshAccessTokenUrl { get; set; }
    [Required]
    public required string ServiceNowUpdateUrl { get; set; }
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string ClientSecret { get; set; }
    [Required]
    public required string RefreshToken { get; set; }
}
