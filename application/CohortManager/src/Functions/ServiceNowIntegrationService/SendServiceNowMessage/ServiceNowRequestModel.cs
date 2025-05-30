namespace NHS.CohortManager.ServiceNowMessageService.Models;

public class ServiceNowRequestModel
{
    public string WorkNotes { get; set; } = string.Empty;
    public int State { get; set; } = 1;
}
