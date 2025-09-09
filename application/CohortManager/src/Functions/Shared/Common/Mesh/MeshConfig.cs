namespace Common;

/// <summary>
/// Root configuration for the shared MESH client and associated mailboxes.
/// </summary>
public class MeshConfig
{
    /// <summary>Azure Key Vault URI for certificate and secret retrieval.</summary>
    public string? KeyVaultConnectionString { get; set; }
    /// <summary>Key Vault secret name or local file path containing server CA certificates.</summary>
    public string? MeshCACertName { get; set; }
    /// <summary>Bypass server certificate validation. Use only for local development.</summary>
    public bool BypassServerCertificateValidation { get; set; } = false;
    /// <summary>Base URL for the MESH API.</summary>
    public required string MeshApiBaseUrl { get; set; }
    /// <summary>Configured mailboxes to register with the client.</summary>
    public required List<MailboxConfig> MailboxConfigs { get; set; }
}

/// <summary>
/// Configuration for a specific MESH mailbox.
/// </summary>
public class MailboxConfig
{
    /// <summary>The MESH mailbox identifier.</summary>
    public required string MailboxId { get; set; }
    /// <summary>Key Vault certificate name or local path for the client certificate.</summary>
    public string? MeshKeyName { get; set; }
    /// <summary>Passphrase for the client certificate, if required.</summary>
    public string? MeshKeyPassword { get; set; }
    /// <summary>Password for the mailbox account.</summary>
    public string? MeshPassword { get; set; }
    /// <summary>Shared key used for HMAC authentication.</summary>
    public required string SharedKey { get; set; }

}
