namespace NHS.Screening.UpdateException;

using System.ComponentModel.DataAnnotations;

public class UpdateExceptionConfig
{
    [Required]
    public string ExceptionManagementDataServiceURL {get; set;}
}
