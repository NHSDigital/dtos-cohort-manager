namespace Common;


using System.ComponentModel.DataAnnotations;

public class JwtTokenServiceConfig
{
    [Required]
    public required string Audience { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string KId { get; set; }

    [Required]
    public required string AuthTokenURL { get; set; }

    public string LocalPrivateKeyFileName { get; set; } = null!;

    public string PrivateKey { get; set; } = null!;

    public string KeyVaultConnectionString { get; set; } = null!;

    public string KeyNamePrivateKey { get; set; } = null!;

    public string KeyNameAPIKey { get; set; } = null!;
}