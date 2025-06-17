namespace NHS.Screening.RemoveValidationException;

using System.ComponentModel.DataAnnotations;

public class RemoveValidationExceptionConfig
{
    [Required]
    public string ExceptionManagementDataServiceURL {get; set;}
    [Required]
    public string DemographicDataServiceURL {get; set;}
}
