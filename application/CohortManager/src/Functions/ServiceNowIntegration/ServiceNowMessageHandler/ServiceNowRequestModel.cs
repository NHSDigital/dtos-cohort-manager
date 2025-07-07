namespace NHS.CohortManager.ServiceNowIntegrationService;

public class ServiceNowRequestModel
{
    public required string WorkNotes { get; set; }
    public int State { get; set; }
}
