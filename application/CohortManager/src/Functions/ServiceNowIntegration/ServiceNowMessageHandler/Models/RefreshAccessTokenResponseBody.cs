namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Text.Json.Serialization;

public class RefreshAccessTokenResponseBody
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
}
