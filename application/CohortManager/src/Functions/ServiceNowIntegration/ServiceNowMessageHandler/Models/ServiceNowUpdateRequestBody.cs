namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;

public class ServiceNowUpdateRequestBody
{
    [JsonPropertyName("state")]
    public int State { get; set; }
    [JsonPropertyName("work_notes")]
    public string? WorkNotes { get; set; }
}
