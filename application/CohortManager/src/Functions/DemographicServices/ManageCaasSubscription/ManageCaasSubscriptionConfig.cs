namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

// Minimal config object for ManageCaasSubscription.
public class ManageCaasSubscriptionConfig
{
    // Optional URL for pass-through to the existing NEMS data service
    // Example: http://manage-nems-subscription:9081/api/NemsSubscriptionDataService
    public string? ManageNemsSubscriptionDataServiceURL { get; set; }

    // Optional base URL to forward selected endpoints (e.g., CheckSubscriptionStatus)
    // Example: http://manage-nems-subscription:9081
    public string? ManageNemsSubscriptionBaseURL { get; set; }

    // Required CAAS mailboxes for the subscribe flow
    [Required]
    public string? CaasToMailbox { get; set; }
    [Required]
    public string? CaasFromMailbox { get; set; }

    // Controls whether shared implementations should use stubbed behavior
    public bool IsStubbed { get; set; } = true;
}
