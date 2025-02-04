/// <summary>Demographic fields required in requests from the BI product.</summary>

namespace NHS.CohortManager.DemographicServices;

public class FilteredDemographicData
{
    public string? PrimaryCareProvider { get; set; }
    public string? PreferredLanguage { get; set; }
}
