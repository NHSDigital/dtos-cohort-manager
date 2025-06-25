namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class ReceiveServiceNowMessageRequestBody
{
    [NotNullOrEmpty]
    [Required]
    [JsonPropertyName("forename")]
    public required string FirstName { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("surname_family_name")]
    public required string FamilyName { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("nhs_number")]
    public required string NhsNumber { get; set; }
    [NotNullOrEmpty]
    [JsonPropertyName("date_of_birth")]
    public required DateOnly DateOfBirth { get; set; }
}
