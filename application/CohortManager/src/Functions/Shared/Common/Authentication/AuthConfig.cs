namespace Common;

using System.ComponentModel.DataAnnotations;

public class AuthConfig
{
    [Required]
    public required string MetaDataUrl { get; init; }
    [Required]
    public required string ClientId { get; init; }

}

