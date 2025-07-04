namespace NHS.CohortManager.CohortDistributionServices;

using System.Text.Json.Serialization;

public class AllocationConfigData
{
    [JsonPropertyName("postcode")]
    public required string Postcode { get; set; }
    [JsonPropertyName("screeningService")]
    public required string ScreeningService { get; set; }
    [JsonPropertyName("serviceProvider")]
    public required string ServiceProvider { get; set; }
}
