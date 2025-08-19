namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class ServiceNowMessageHandlerConfig
{
    [Required]
    public required string ServiceNowRefreshAccessTokenUrl { get; set; }
    [Required]
    public required string ServiceNowUpdateUrl { get; set; }
    [Required]
    public required string ServiceNowResolutionUrl { get; set; }
    [Required]
    public required string ServiceNowGrantType { get; set; }
    [Required]
    public required string ServiceNowClientId { get; set; }
    [Required]
    public required string ServiceNowClientSecret { get; set; }
    /// <remarks>
    /// When the ServiceNowGrantType is set to 'refresh_token', this property should be set.
    /// </remarks>
    public string? ServiceNowRefreshToken { get; set; }
    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public required string ServiceNowParticipantManagementTopic { get; set; }
    [Required]
    public string ServiceNowAssignmentGroup { get; set; } = "ITO Breast Screening 2nd Line";
}
