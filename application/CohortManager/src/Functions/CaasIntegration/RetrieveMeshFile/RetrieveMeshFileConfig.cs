namespace NHS.Screening.RetrieveMeshFile;

using System.ComponentModel.DataAnnotations;

public class RetrieveMeshFileConfig
{
    public string MeshApiBaseUrl {get; set;}
    [Required]
    public string BSSMailBox {get; set;}
    [Required]
    public string MeshPassword {get; set;}
    [Required]
    public string MeshSharedKey {get; set;}
    public string MeshKeyPassphrase {get; set;}
    [Required]
    public string MeshKeyName {get; set;}

}
