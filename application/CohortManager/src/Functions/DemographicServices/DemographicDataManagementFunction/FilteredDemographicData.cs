/// <summary>Demographic fields required in requests from the BI product.</summary>

namespace NHS.CohortManager.DemographicServices;

using Model;

public class FilteredDemographicData
{
    public FilteredDemographicData() {}
    public FilteredDemographicData(ParticipantDemographic dbDemographic)
    {
        PrimaryCareProvider = dbDemographic.PrimaryCareProvider;
        PreferredLanguage = dbDemographic.PreferredLanguage;
    }

    public string? PrimaryCareProvider { get; set; }
    public string? PreferredLanguage { get; set; }
}
