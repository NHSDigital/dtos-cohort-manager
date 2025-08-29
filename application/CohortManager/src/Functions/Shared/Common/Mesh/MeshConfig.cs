namespace Common;

public class MeshConfig
{
    public string? KeyVaultConnectionString { get; set; }
    public string? MeshCACertName { get; set; }
    public bool BypassServerCertificateValidation { get; set; } = false;
    public required string MeshApiBaseUrl { get; set; }
    public required List<MailboxConfig> MailboxConfigs { get; set; }
}

public class MailboxConfig
{
    public required string MailboxId { get; set; }
    public string? MeshKeyName { get; set; }
    public string? MeshKeyPassword { get; set; }
    public string? MeshPassword { get; set; }
    public required string SharedKey { get; set; }

}
