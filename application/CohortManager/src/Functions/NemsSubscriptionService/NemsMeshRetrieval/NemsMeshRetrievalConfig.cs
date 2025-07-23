namespace NHS.Screening.NemsMeshRetrieval;

using System.ComponentModel.DataAnnotations;

public class NemsMeshRetrievalConfig
{
    public string MeshApiBaseUrl { get; set; }
    [Required]
    public string BSSMailBox { get; set; }
    [Required]
    public string MeshPassword { get; set; }
    [Required]
    public string MeshSharedKey {get; set;}
    public string MeshKeyPassphrase {get; set;}
    public string MeshKeyName {get; set;}
    public string KeyVaultConnectionString {get; set;}
    [Required]
    public string nemsmeshfolder_STORAGE {get; set;}
    public string InboundContainer { get; set; } = "nems-updates";
    public string ConfigContainer { get; set; } = "nems-config";
    public string ServerSideCerts { get; set; }
    public string MeshCertName { get; set; }
    public bool? BypassServerCertificateValidation {get;set;}
}
