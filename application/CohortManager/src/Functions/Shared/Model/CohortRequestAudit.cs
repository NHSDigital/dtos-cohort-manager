namespace Model;

using System.Text.Json.Serialization;

public class CohortRequestAudit
    {
        [JsonPropertyName("REQUEST_ID")]
        public string? RequestId { get; set; }
        [JsonPropertyName("STATUS_CODE")]
        public string? StatusCode { get; set; }
        [JsonPropertyName("CREATED_DATETIME")]
        public string? CreatedDateTime { get; set; }
    }
