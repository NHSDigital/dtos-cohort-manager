namespace NHS.CohortManager.ServiceNowMessageService;

using System.ComponentModel.DataAnnotations;

public class SendServiceNowMsgConfig
{
    [Required]
    public required string UpdateEndpoint { get; set; }
    [Required]
    public required string AccessToken { get; set; }

    public required string ServiceNowBaseUrl { get; set; }
    public required string Profile { get; set; }
    public required string Definition { get; set; }

    public required string EndpointPath { get; set; }
}
