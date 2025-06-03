namespace NHS.CohortManager.ServiceNowMessageService.Models;

public class ServiceNowRequestModel
{
    public required string WorkNotes { get; set; }
    public int State { get; set; }
}

