namespace NHS.Screening.RemoveValidationException;

using System.ComponentModel.DataAnnotations;

public class RemoveValidationExceptionConfig
{
    [Required]
    public required string ExceptionManagementDataServiceURL {get; set;}
    [Required]
    public required string DemographicDataServiceURL {get; set;}
}
