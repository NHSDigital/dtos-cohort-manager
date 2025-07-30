
namespace Common;

using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class JwtTokenExtension
{

    /// <summary>
    /// gets the private key from the local dir or from keyvault if the function is running in azure
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IHostBuilder AddJwtTokenSigning(this IHostBuilder hostBuilder, bool useFakeBearerTokenService = false)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("program.cs");

        JwtPrivateKey jwtPrivateKey;
        try
        {
            // Azure   
            hostBuilder.AddConfiguration<JwtTokenServiceConfig>(out JwtTokenServiceConfig config);
            if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
            {
                var certClient = new CertificateClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
                Response<X509Certificate2> certResponse = certClient.DownloadCertificate(config.KeyNamePrivateKey);

                logger.LogInformation("got certificate from key vault");
                var stringCert = CertificateToString(certResponse.Value);

                if (string.IsNullOrEmpty(stringCert))
                {
                    throw new ArgumentException("The private key was null or empty");
                }
                jwtPrivateKey = new JwtPrivateKey(stringCert);
            }
            // Local
            else
            {
                jwtPrivateKey = new JwtPrivateKey(GetPrivateKey(config.LocalPrivateKeyFileName));
            }

            var host = hostBuilder.ConfigureServices(_ =>
            {
                _.AddMemoryCache();
                _.AddSingleton(jwtPrivateKey);
                _.AddSingleton<IAuthorizationClientCredentials, AuthorizationClientCredentials>();
                _.AddSingleton<IJwtTokenService, JwtTokenService>();
                _.AddSingleton<ISigningCredentialsProvider, SigningCredentialsProvider>();
                if (!useFakeBearerTokenService)
                {
                    _.AddSingleton<IBearerTokenService, BearerTokenService>();
                }
                else
                {
                    _.AddSingleton<IBearerTokenService, BearerTokenServiceMock>();
                }
            });

            return host;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            throw;
        }

    }

    private static string CertificateToString(X509Certificate2 certificate)
    {
        using RSA? rsa = certificate.GetRSAPrivateKey();
        if (rsa == null)
        {
            return "";
        }

        byte[] pkcs8PrivateKey = rsa!.ExportPkcs8PrivateKey();
        return Convert.ToBase64String(pkcs8PrivateKey);

    }


    private static string GetPrivateKey(string privateKeyFilePath)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var filePath = Path.Combine(currentDirectory, privateKeyFilePath);

        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        string keyContent = File.ReadAllText(filePath);
        return keyContent;
    }
}
