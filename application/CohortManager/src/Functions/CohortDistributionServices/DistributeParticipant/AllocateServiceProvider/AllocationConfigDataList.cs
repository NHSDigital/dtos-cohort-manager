namespace NHS.CohortManager.CohortDistributionServices;

using System.Text.Json.Serialization;

public class AllocationConfigDataList
{
    [JsonPropertyName("allocation_config_data_list")]
    public AllocationConfigData[]? ConfigDataList { get; set; }

}
