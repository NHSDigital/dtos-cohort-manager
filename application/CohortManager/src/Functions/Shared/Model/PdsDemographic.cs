namespace Model;

using System.Text.Json.Serialization;

public class PdsDemographic : Demographic
{
    [JsonPropertyOrder(900)]
    public string? ReasonForRemoval { get; set; }
    [JsonPropertyOrder(901)]
    public string? EffectiveFromDate { get; set; }
    [JsonPropertyOrder(902)]
    public string? EffectiveToDate { get; set; }
    [JsonPropertyOrder(903)]
    public string? ConfidentialityCode { get; set; } = "";
    public PdsDemographic() { }
}
