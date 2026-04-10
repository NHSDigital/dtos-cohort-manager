namespace Common;

using System.ComponentModel.DataAnnotations;

public class AuthConfig
{
    [Required, Url]
    public required string AuthMetaDataUrl { get; init; }
    [Required]
    public required string AuthClientId { get; init; }
    [Required, Url]
    public required string UserInfoUrl { get; init; }

}

