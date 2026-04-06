namespace Common;

using System.ComponentModel.DataAnnotations;

public class HttpValidationConfig
{
    [Required]
    public required string ExceptionFunctionURL { get; set; } = null!;
}