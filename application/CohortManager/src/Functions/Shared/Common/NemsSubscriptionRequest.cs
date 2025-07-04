namespace Common;

using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Base request object for NEMS subscription operations
/// </summary>
public class NemsSubscriptionRequest
{
    /// <summary>
    /// The URL for the NEMS subscription endpoint
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// JWT token for authentication
    /// </summary>
    public string JwtToken { get; set; } = string.Empty;

    /// <summary>
    /// Source ASID (Application Service Identifier)
    /// </summary>
    public string FromAsid { get; set; } = string.Empty;

    /// <summary>
    /// Target ASID (Application Service Identifier)
    /// </summary>
    public string ToAsid { get; set; } = string.Empty;

    /// <summary>
    /// Client certificate for mutual TLS authentication
    /// </summary>
    public X509Certificate2 ClientCertificate { get; set; } = null!;

    /// <summary>
    /// Whether to bypass server certificate validation (development only)
    /// </summary>
    public bool BypassCertValidation { get; set; }
}

/// <summary>
/// Request object for NEMS subscription POST operations (inherits common properties)
/// </summary>
public class NemsSubscriptionPostRequest : NemsSubscriptionRequest
{
    /// <summary>
    /// JSON representation of the subscription to create
    /// </summary>
    public string SubscriptionJson { get; set; } = string.Empty;
}