namespace Common;

using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Hl7.Fhir.Model.CdsHooks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client;
using NHS.MESH.Client.Configuration;
/// <summary>
/// Service registration helpers for configuring MESH clients and mailboxes.
/// </summary>
public static class MeshMailboxExtension
{
    private static ILogger _logger;
    /// <summary>
    /// Registers the MESH client and configured mailboxes for dependency injection.
    /// </summary>
    /// <param name="hostBuilder">The host builder to extend.</param>
    /// <param name="config">Configuration for MESH and mailboxes.</param>
    /// <returns>The original host builder.</returns>
    public static IHostBuilder AddMeshMailboxes(this IHostBuilder hostBuilder, MeshConfig config)
    {
        ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
        _logger = factory.CreateLogger("MeshMailboxExtension");

        hostBuilder.ConfigureServices(async services =>
        {
            var meshClientBuilder = services.AddMeshClient(_ =>
            {
                _.MeshApiBaseUrl = config.MeshApiBaseUrl;
                _.BypassServerCertificateValidation = config.BypassServerCertificateValidation;
            });

            foreach (var mailbox in config.MailboxConfigs)
            {
                var cert = await GetCertificate(mailbox.MeshKeyName, mailbox.MeshKeyPassword, config.KeyVaultConnectionString);
                var serverSideCerts = await GetCACertificates(config.MeshCACertName, config.KeyVaultConnectionString);
                meshClientBuilder.AddMailbox(mailbox.MailboxId, new MailboxConfiguration
                {
                    Password = mailbox.MeshPassword,
                    SharedKey = mailbox.SharedKey,
                    Cert = cert,
                    serverSideCertCollection = serverSideCerts
                });
            }

            services = meshClientBuilder.Build();

        });

        return hostBuilder;
    }

    private static async Task<X509Certificate2?> GetCertificate(string meshKeyName, string? meshKeyPassphrase, string? keyVaultConnectionString)
    {
        if (!string.IsNullOrEmpty(keyVaultConnectionString))
        {
            _logger.LogInformation("Pulling Mesh Certificate from KeyVault");
            var certClient = new CertificateClient(vaultUri: new Uri(keyVaultConnectionString), credential: new DefaultAzureCredential());
            var certificate = await certClient.DownloadCertificateAsync(meshKeyName);
            return certificate.Value;
        }

        if (!string.IsNullOrEmpty(meshKeyName) || Path.Exists(meshKeyName))
        {
            _logger.LogInformation("Pulling Mesh Certificate from local File");
            return new X509Certificate2(meshKeyName!, meshKeyPassphrase);
        }
        return null;
    }


    /// <summary>
    /// Retrieves server-side CA certificates from Key Vault or a local file path.
    /// </summary>
    /// <param name="meshCertName">Key Vault secret name or file path containing PEM certificates.</param>
    /// <param name="keyVaultConnectionString">Optional Key Vault URI.</param>
    /// <returns>A certificate collection when available; otherwise null.</returns>
    public static async Task<X509Certificate2Collection?> GetCACertificates(string meshCertName, string? keyVaultConnectionString)
    {
        if (!string.IsNullOrEmpty(keyVaultConnectionString))
        {
            var secretClient = new SecretClient(vaultUri: new Uri(keyVaultConnectionString), credential: new DefaultAzureCredential());
            string base64Cert = secretClient.GetSecret(meshCertName).Value.Value;
            return CertificateHelper.GetCertificatesFromString(base64Cert);
        }
        if (!string.IsNullOrEmpty(meshCertName) || Path.Exists(meshCertName))
        {

            string certsString = await File.ReadAllTextAsync(meshCertName);
            return CertificateHelper.GetCertificatesFromString(certsString);
        }
        return null;
    }
}
