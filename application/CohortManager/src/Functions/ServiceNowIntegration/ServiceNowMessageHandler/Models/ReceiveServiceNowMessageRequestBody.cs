namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.Text.Json.Serialization;
using Common;

// TODO: Check json property names and update where required once schema has been updated
public class ReceiveServiceNowMessageRequestBody
{
    [NotNullOrEmpty]
    [JsonPropertyName("forename_")]
    public required string FirstName { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("surname_family_name")]
    public required string FamilyName { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("nhs_number")]
    public required string NhsNumber { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("date_of_birth")]
    public required string DateOfBirth { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("BSO_code")]
    public required string BsoCode { get; set; }
    [JsonPropertyName("required_gp_code")]
    public string? RequiredGpCode { get; set; }
}
