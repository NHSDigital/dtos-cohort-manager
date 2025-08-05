namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Model;

public class ServiceNowVariableData
{
    [Required]
    [JsonPropertyName("forename_")]
    public required string FirstName { get; set; }
    [Required]
    [JsonPropertyName("surname_family_name")]
    public required string FamilyName { get; set; }
    [Required]
    [JsonPropertyName("nhs_number")]
    public required string NhsNumber { get; set; }
    [JsonPropertyName("date_of_birth")]
    public required DateOnly DateOfBirth { get; set; }
    [Required]
    [JsonPropertyName("BSO_code")]
    public required string BsoCode { get; set; }
    [JsonPropertyName("reason_for_adding")]
    [AllowedValues([ServiceNowReasonsForAdding.VeryHighRisk, ServiceNowReasonsForAdding.RequiresCeasing, ServiceNowReasonsForAdding.RoutineScreening])]
    public required string ReasonForAdding { get; set; }
    [JsonPropertyName("enter_dummy_gp_code")]
    public string? RequiredGpCode { get; set; }
}
