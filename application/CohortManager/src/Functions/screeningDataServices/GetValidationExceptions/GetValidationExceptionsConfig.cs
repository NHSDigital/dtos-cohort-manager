namespace NHS.Screening.GetValidationExceptions;

using System.ComponentModel.DataAnnotations;

public class GetValidationExceptionsConfig
{
    [Required]
    public string ExceptionManagementDataServiceURL {get; set;}
    [Required]
    public string DemographicDataServiceURL {get; set;}
}
