namespace NHS.CohortManager.ServiceNowMessageService;

using System.ComponentModel.DataAnnotations;

public class SendServiceNowMsgConfig
{
    [Required]
    public string UpdateEndpoint { get; set; }
    [Required]
    public string AccessToken { get; set; }

    public string ServiceNowBaseUrl { get; set; }
    public string Profile { get; set; }
    public string Definition { get; set; }

    public string EndpointPath { get; set; }
}
