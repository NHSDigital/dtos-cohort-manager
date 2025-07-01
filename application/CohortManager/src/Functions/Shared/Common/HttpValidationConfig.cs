namespace Common;

using System.ComponentModel.DataAnnotations;

public class HttpValidationConfig
{
    [Required]
    public string ExceptionFunctionURL { get; set; } = null!;
}