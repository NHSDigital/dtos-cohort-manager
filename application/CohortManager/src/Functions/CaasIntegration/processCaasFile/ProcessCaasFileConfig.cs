using System.ComponentModel.DataAnnotations;

public class ProcessCaasFileConfig
{
    [Required]
    public string PMSAddParticipant {get;set;}
    [Required]
    public string PMSRemoveParticipant {get;set;}
    [Required]
    public string PMSUpdateParticipant {get;set;}
    [Required]
    public string DemographicURI {get;set;}
    [Required]
    public string ExceptionFunctionURL {get;set;}
}
