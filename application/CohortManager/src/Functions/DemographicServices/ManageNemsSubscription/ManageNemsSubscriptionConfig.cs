namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

public class ManageNemsSubscriptionConfig
{
    [Required]
    public string NemsFhirEndpoint { get; set; }
    [Required]
    public string RetrievePdsDemographicURL { get; set; }
    public string SpineAccessToken { get; set; }
    public string FromAsid { get; set; }
    public string ToAsid { get; set; }
    public string SubscriptionProfile { get; set; }
    public string SubscriptionCriteria { get; set; }
    public string CallbackEndpoint { get; set; }
    public string CallAuthToken { get; set; }
    public string NemsDeleteEndpoint { get; set; }
}
