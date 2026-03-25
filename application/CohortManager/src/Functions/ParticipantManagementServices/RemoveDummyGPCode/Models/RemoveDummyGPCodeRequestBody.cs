namespace NHS.CohortManager.ParticipantManagementServices.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class RemoveDummyGPCodeRequestBody
{
    [Required]
    [JsonPropertyName("nhs_number")]
    public required string NhsNumber { get; set; }

    [Required]
    [JsonPropertyName("forename")]
    public required string Forename { get; set; }

    [Required]
    [JsonPropertyName("surname")]
    public required string Surname { get; set; }

    [Required]
    [JsonPropertyName("date_of_birth")]
    public required DateOnly DateOfBirth { get; set; }

    [Required]
    [JsonPropertyName("request_id")]
    public required string RequestId { get; set; }
}
