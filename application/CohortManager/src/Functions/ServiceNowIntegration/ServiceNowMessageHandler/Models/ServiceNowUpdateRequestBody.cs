namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;

public class ServiceNowUpdateRequestBody
{
    [JsonPropertyName("state")]
    public int State { get; set; }
    [JsonPropertyName("work_notes")]
    public string? WorkNotes { get; set; }
    [JsonPropertyName("needs_attention")]
    public bool? NeedsAttention { get; set; }
    [JsonPropertyName("assignment_group")]
    public string? AssignmentGroup { get; set; }
}
