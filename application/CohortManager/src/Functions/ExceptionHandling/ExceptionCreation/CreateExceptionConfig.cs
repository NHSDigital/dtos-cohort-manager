namespace NHS.Screening.CreateException;

using System.ComponentModel.DataAnnotations;

public class CreateExceptionConfig
{
    [Required]
    public string ExceptionManagementDataServiceURL {get; set;}
    [Required]
    public string DemographicDataServiceURL {get; set;}
}
