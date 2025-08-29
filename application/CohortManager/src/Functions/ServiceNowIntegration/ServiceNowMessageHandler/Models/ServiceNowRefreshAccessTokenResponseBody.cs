namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;

public class ServiceNowRefreshAccessTokenResponseBody
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }
    /// <value>
    /// An int representing the number of seconds the AccessToken expires in
    /// </value>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
