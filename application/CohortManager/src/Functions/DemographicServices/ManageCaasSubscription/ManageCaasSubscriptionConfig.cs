namespace NHS.CohortManager.DemographicServices;

// Minimal config object for ManageCaasSubscription.
// Intentionally no [Required] attributes so binding does not fail if unset.
public class ManageCaasSubscriptionConfig
{
    // Optional URL for pass-through to the existing NEMS data service
    // Example: http://manage-nems-subscription:9081/api/NemsSubscriptionDataService
    public string? ManageNemsSubscriptionDataServiceURL { get; set; }

    // Optional base URL to forward selected endpoints (e.g., CheckSubscriptionStatus)
    // Example: http://manage-nems-subscription:9081
    public string? ManageNemsSubscriptionBaseURL { get; set; }

    // Optional CAAS mailboxes for the subscribe stub
    public string? CaasToMailbox { get; set; }
    public string? CaasFromMailbox { get; set; }

    // Controls whether shared implementations should use stubbed behavior
    public bool IsStubbed { get; set; } = true;
}
