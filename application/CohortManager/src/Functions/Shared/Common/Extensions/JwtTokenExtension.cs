
namespace Common;

using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Hosting;

public static class JwtTokenExtension
{
    /// <summary>
    /// gets the private key from the local dir or from keyvault if the function is running in azure
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IHostBuilder GetPrivateKey(this IHostBuilder hostBuilder, JwtTokenServiceConfig config)
    {
        // Azure
        if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
        {
            var certClient = new CertificateClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
            var keyVaultClient = new SecretClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());

            var privateKey = certClient.DownloadCertificate(config.KeyNamePrivateKey);
            var APIKey = keyVaultClient.GetSecret(config.KeyNameAPIKey);

            config.PrivateKey = CertificateToString(privateKey.Value);
            // this gets the actual value in string format
            config.ClientId = APIKey.Value.Value;
        }
        // Local
        else
        {
            config.PrivateKey = GetPrivateKey(config.LocalPrivateKeyFileName);
        }

        return hostBuilder;
    }

    private static string CertificateToString(X509Certificate2 certificate)
    {
        byte[] certData = certificate.Export(X509ContentType.Cert);
        return Convert.ToBase64String(certData);
    }


    private static string GetPrivateKey(string privateKeyFilePath)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        currentDirectory = currentDirectory.Replace("bin/output", "");

        var filePath = Path.Combine(currentDirectory, privateKeyFilePath);

        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        string keyContent = File.ReadAllText(filePath);
        return keyContent;
    }
}
