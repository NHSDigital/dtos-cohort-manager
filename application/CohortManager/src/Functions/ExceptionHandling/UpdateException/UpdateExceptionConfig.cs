namespace NHS.Screening.UpdateException;

using System.ComponentModel.DataAnnotations;

public class UpdateExceptionConfig
{
    [Required]
    public required string ExceptionManagementDataServiceURL {get; set;}
}
