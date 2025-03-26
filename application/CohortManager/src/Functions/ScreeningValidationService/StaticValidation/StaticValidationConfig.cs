namespace NHS.Screening.StaticValidation;

using System.ComponentModel.DataAnnotations;

public class StaticValidationConfig
{
    [Required]
    public string RemoveOldValidationRecord { get; set; }
}