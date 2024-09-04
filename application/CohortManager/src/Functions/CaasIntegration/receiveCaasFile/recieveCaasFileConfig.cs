using System.ComponentModel.DataAnnotations;

public class receiveCaasFileConfig
{
    [Required]
    public string caasfolder_STORAGE {get;set;}
    [Required]
    public string ProcessCaasFileUri {get; set;}
    [Required]
    public string FileValidationUri {get;set;}
    [Required]
    public string Dt0sDatabaseConnectionString {get;set;}

}
