namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;

public class ServiceNowResolutionRequestBody
{
    [JsonPropertyName("state")]
    public int State { get; set; }
    [JsonPropertyName("resolution_code")]
    public required string ResolutionCode { get; set; }
    [JsonPropertyName("close_notes")]
    public required string CloseNotes { get; set; }
    [JsonPropertyName("work_notes")]
    public string? WorkNotes { get; set; }
}
