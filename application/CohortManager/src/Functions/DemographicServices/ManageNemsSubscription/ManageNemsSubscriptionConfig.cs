namespace NHS.CohortManager.DemographicServices.ManageNemsSubscription.Config;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for NEMS subscription management
/// </summary>
public class ManageNemsSubscriptionConfig
{
    /// <summary>
    /// NEMS FHIR API base endpoint
    /// Integration: https://msg.intspineservices.nhs.uk/STU3
    /// Production: Contact NHS Digital for production URLs
    /// </summary>
    [Required(ErrorMessage = "NemsFhirEndpoint is required")]
    public string NemsFhirEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Your organization's ASID (Application Service Instance Identifier)
    /// Example: "200000002527"
    /// </summary>
    [Required(ErrorMessage = "FromAsid is required")]
    public string FromAsid { get; set; } = string.Empty;

    /// <summary>
    /// Target ASID (usually same as FromAsid for NEMS)
    /// Example: "200000002527"
    /// </summary>
    [Required(ErrorMessage = "ToAsid is required")]
    public string ToAsid { get; set; } = string.Empty;

    /// <summary>
    /// Your organization's ODS code
    /// Example: "T8T9T"
    /// </summary>
    [Required(ErrorMessage = "OdsCode is required")]
    public string OdsCode { get; set; } = string.Empty;

    /// <summary>
    /// Your MESH mailbox ID (CRITICAL: Use full mailbox ID, not just ODS code)
    /// Example: "T8T9TOT001" (NOT just "T8T9T")
    /// </summary>
    [Required(ErrorMessage = "MeshMailboxId is required")]
    public string MeshMailboxId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Key Vault connection string for certificate storage
    /// Example: "https://your-keyvault.vault.azure.net/"
    /// </summary>
    public string KeyVaultConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the NEMS client certificate stored in Key Vault
    /// Example: "nems-client-certificate"
    /// </summary>
    public string NemsKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Local certificate file path (for development only)
    /// Only used when KeyVaultConnectionString is empty
    /// </summary>
    public string? NemsLocalCertPath { get; set; }

    /// <summary>
    /// Local certificate password (for development only)
    /// Only used when KeyVaultConnectionString is empty
    /// </summary>
    public string? NemsLocalCertPassword { get; set; }

    /// <summary>
    /// FHIR profile for EMS subscriptions
    /// </summary>
    public string SubscriptionProfile { get; set; } = "https://fhir.nhs.uk/STU3/StructureDefinition/EMS-Subscription-1";

    /// <summary>
    /// Base criteria for NHS number identifier
    /// </summary>
    public string SubscriptionCriteria { get; set; } = "https://fhir.nhs.uk/Id/nhs-number";

    /// <summary>
    /// Whether to bypass server certificate validation (for development only)
    /// Set to true only during local development
    /// </summary>
    public bool BypassServerCertificateValidation { get; set; } = false;

    /// <summary>
    /// Default event types to subscribe to
    /// </summary>
    public string[] DefaultEventTypes { get; set; } = new[]
    {
        "pds-record-change-1"
    };

    /// <summary>
    /// Custom validation to ensure either KeyVault or local cert is configured
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(KeyVaultConnectionString) || !string.IsNullOrEmpty(NemsLocalCertPath);
}
