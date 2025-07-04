namespace Common;

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

public class NemsHttpClientProvider : INemsHttpClientProvider
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<NemsHttpClientProvider> _logger;

    public NemsHttpClientProvider(IHttpClientFactory factory, ILogger<NemsHttpClientProvider> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public HttpClient CreateClient(X509Certificate2? clientCertificate = null, bool bypassCertValidation = false)
    {
        if (clientCertificate == null && !bypassCertValidation)
        {
            return _factory.CreateClient();
        }

        var handler = ConfigureNemsHttpClientHandler(clientCertificate, bypassCertValidation);
        return new HttpClient(handler);
    }

    private HttpClientHandler ConfigureNemsHttpClientHandler(
        X509Certificate2? clientCertificate = null,
        bool bypassCertValidation = false)
    {
        var handler = new HttpClientHandler();

        // Add client certificate for mutual TLS authentication
        if (clientCertificate != null)
        {
            handler.ClientCertificates.Add(clientCertificate);
            _logger.LogInformation("Added client certificate for NEMS authentication");
        }

        if (bypassCertValidation)
        {
            _logger.LogWarning("Certificate validation bypass requested - USE ONLY IN DEVELOPMENT");
            
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                _logger.LogWarning("Bypassing server certificate validation - DO NOT USE IN PRODUCTION");

                // Still perform basic certificate validation even when bypassing
                if (cert == null)
                {
                    _logger.LogError("Server certificate is null");
                    return false;
                }

                // Check if certificate is expired
                if (cert.NotAfter < DateTime.Now || cert.NotBefore > DateTime.Now)
                {
                    _logger.LogError("Server certificate is expired or not yet valid");
                    return false;
                }

                return true;
            };
        }

        return handler;
    }
}
