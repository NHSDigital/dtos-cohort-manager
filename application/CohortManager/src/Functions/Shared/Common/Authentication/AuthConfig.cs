namespace Common;

using System.ComponentModel.DataAnnotations;

public class AuthConfig
{
    [Required]
    public required string AuthMetaDataUrl { get; init; }
    [Required]
    public required string AuthClientId { get; init; }

}

