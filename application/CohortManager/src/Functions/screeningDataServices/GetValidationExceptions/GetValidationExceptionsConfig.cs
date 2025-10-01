namespace NHS.Screening.GetValidationExceptions;

using System.ComponentModel.DataAnnotations;

public class GetValidationExceptionsConfig
{
    [Required]
    public required string ExceptionManagementDataServiceURL {get; set;}
    [Required]
    public required string DemographicDataServiceURL {get; set;}
}
