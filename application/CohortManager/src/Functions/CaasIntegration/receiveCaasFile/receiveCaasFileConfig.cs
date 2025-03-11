namespace NHS.Screening.ReceiveCaasFile;

using System.ComponentModel.DataAnnotations;

public class ReceiveCaasFileConfig
{
    [Required]
    public string DemographicDataServiceURL {get; set;}
    [Required]
    public string ScreeningLkpDataServiceURL {get; set;}
    [Required]
    public string DemographicURI {get; set;}
    [Required]
    public int BatchSize {get; set;}
}
