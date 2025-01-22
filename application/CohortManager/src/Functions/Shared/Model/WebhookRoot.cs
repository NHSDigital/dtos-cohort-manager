namespace Model;

using System.Text.Json.Serialization;

public class WebhookResponse
{

    [JsonPropertyName("runtimeStatus")]
    public string? RuntimeStatus { get; set; }

    [JsonPropertyName("createdTime")]
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("lastUpdatedTime")]
    public DateTime LastUpdatedTime { get; set; }
}
