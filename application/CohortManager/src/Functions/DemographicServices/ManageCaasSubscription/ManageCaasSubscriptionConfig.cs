namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for the ManageCaasSubscription function app and MESH connectivity.
/// </summary>
public class ManageCaasSubscriptionConfig
{
    /// <summary>Base URL for the MESH API.</summary>
    [Required]
    public string MeshApiBaseUrl { get; set; }
    /// <summary>Optional Azure Key Vault URL for certificate and secret retrieval.</summary>
    public string? KeyVaultConnectionString { get; set; }
    /// <summary>Bypass server certificate validation for local/dev purposes.</summary>
    public bool BypassServerCertificateValidation { get; set; } = false;
    /// <summary>Key Vault secret name or local path for CA certificates used to validate the MESH server.</summary>
    public string? MeshCACertName { get; set; }
    /// <summary>Key Vault certificate name or local path for the client certificate.</summary>
    public string? MeshCaasKeyName { get; set; }
    /// <summary>Passphrase for the client certificate, if required.</summary>
    public string? MeshCaasKeyPassword { get; set; }
    /// <summary>MESH mailbox password for client authentication.</summary>
    public string? MeshCaasPassword { get; set; }
    /// <summary>MESH shared key for HMAC authentication.</summary>
    [Required]
    public string MeshCaasSharedKey { get; set; }
    /// <summary>Destination mailbox for sending CAAS subscription messages.</summary>
    [Required]
    public string CaasToMailbox { get; set; }
    /// <summary>Source mailbox used for sending CAAS subscription messages.</summary>
    [Required]
    public string CaasFromMailbox { get; set; }

    /// <summary>Controls whether shared implementations use stubbed behavior.</summary>
    public bool IsStubbed { get; set; } = true;
}
