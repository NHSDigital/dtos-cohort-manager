namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

public class SendServiceNowMessageRequestBody
{
    public int State { get; set; }
    public string? WorkNotes { get; set; }
}
