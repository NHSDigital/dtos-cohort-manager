namespace NHS.CohortManager.ServiceNowMessageService;

using System.ComponentModel.DataAnnotations;

public class SendServiceNowMsgConfig
{
    [Required]
    public string UpdateEndpoint { get; set; }= string.Empty;
    [Required]
    public string AccessToken { get; set; }= string.Empty;

    public string ServiceNowBaseUrl { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}
