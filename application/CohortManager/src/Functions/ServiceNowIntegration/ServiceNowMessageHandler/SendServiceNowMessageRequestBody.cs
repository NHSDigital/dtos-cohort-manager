namespace NHS.CohortManager.ServiceNowIntegrationService;

public class SendServiceNowMessageRequestBody
{
    public required string WorkNotes { get; set; }
    public int State { get; set; }
}
