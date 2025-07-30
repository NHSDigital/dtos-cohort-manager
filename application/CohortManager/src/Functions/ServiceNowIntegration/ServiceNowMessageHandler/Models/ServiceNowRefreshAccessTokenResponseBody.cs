namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;

public class ServiceNowRefreshAccessTokenResponseBody
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
}
