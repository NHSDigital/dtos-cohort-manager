namespace NHS.CohortManager.DemographicServices;

using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Extensions.Logging;



public static class CertificateExtensions
{
    /// <summary>
    /// Loads the NEMS certificate from either Azure Key Vault or local file system
    /// </summary>
    /// <param name="config">The NEMS subscription configuration</param>
    /// <param name="logger">Logger for diagnostic messages</param>
    /// <returns>The loaded X509Certificate2</returns>
    /// <exception cref="InvalidOperationException">Thrown when no certificate configuration is found</exception>
    public static async Task<X509Certificate2> LoadNemsCertificateAsync(this ManageNemsSubscriptionConfig config, ILogger logger)
    {
        if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
        {
            logger.LogInformation("Loading NEMS certificate from Azure Key Vault");
            var certClient = new CertificateClient(
                new Uri(config.KeyVaultConnectionString),
                new ManagedIdentityCredential()
            );
            var certResult = await certClient.DownloadCertificateAsync(config.NemsKeyName);
            return certResult.Value;
        }

        if (!string.IsNullOrEmpty(config.NemsLocalCertPath))
        {
            logger.LogInformation("Loading NEMS certificate from local file");
            return !string.IsNullOrEmpty(config.NemsLocalCertPassword)
                ? new X509Certificate2(config.NemsLocalCertPath, config.NemsLocalCertPassword)
                : new X509Certificate2(config.NemsLocalCertPath);

        }

        throw new InvalidOperationException("No certificate configuration found. Please configure either KeyVaultConnectionString or NemsLocalCertPath.");
    }
}
