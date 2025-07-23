namespace NHS.CohortManager.ServiceNowIntegrationService.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class ReceiveServiceNowMessageRequestBody
{
    [Required]
    [JsonPropertyName("number")]
    public required string ServiceNowCaseNumber { get; set; }
    [JsonPropertyName("u_case_variable_data")]
    public required ServiceNowVariableData VariableData { get; set; }
}
