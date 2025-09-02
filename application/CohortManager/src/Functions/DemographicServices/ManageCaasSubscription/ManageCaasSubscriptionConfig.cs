namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

// Minimal config object for ManageCaasSubscription.
public class ManageCaasSubscriptionConfig
{
    [Required]
    public string? MeshApiBaseUrl { get; set; }
    public string? KeyVaultConnectionString { get; set; }
    public bool BypassServerCertificateValidation { get; set; } = false;
    public string? MeshCACertName { get; set; }
    public string? MeshCaasKeyName { get; set; }
    public string? MeshCaasKeyPassword { get; set; }
    public string? MeshCaasPassword { get; set; }
    public required string MeshCaasSharedKey { get; set; }
    [Required]
    public string? CaasToMailbox { get; set; }
    [Required]
    public string? CaasFromMailbox { get; set; }

    // Controls whether shared implementations should use stubbed behavior
    public bool IsStubbed { get; set; } = true;
}
