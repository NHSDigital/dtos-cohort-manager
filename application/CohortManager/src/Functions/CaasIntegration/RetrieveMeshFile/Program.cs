using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using NHS.Screening.RetrieveMeshFile;
using HealthChecks.Extensions;


var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");

try
{
    var host = new HostBuilder();

    X509Certificate2 cert = null;
    X509Certificate2Collection caCerts = new X509Certificate2Collection();

    var KeyVaultConnectionString = Environment.GetEnvironmentVariable("KeyVaultConnectionString");

    host.AddConfiguration<RetrieveMeshFileConfig>(out RetrieveMeshFileConfig config);

    if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
    {
        logger.LogInformation("Pulling Mesh Certificate from KeyVault");
        var client = new CertificateClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
        var certificate = await client.DownloadCertificateAsync(config.MeshKeyName);
        cert = certificate.Value;
    }
    else if (!string.IsNullOrEmpty(config.MeshKeyName))
    {
        logger.LogInformation("Pulling Mesh Certificate from local File");
        cert = new X509Certificate2(config.MeshKeyName, config.MeshKeyPassphrase);
    }

    if (!string.IsNullOrEmpty(KeyVaultConnectionString))
    {
        var client = new CertificateClient(new Uri(KeyVaultConnectionString), new DefaultAzureCredential());
        var allCertificates = client.GetPropertiesOfCertificates();

        foreach (var certificateProperties in allCertificates)
        {
            if (certificateProperties.Name.StartsWith("CaCert"))
            {
                var certificateWithPolicy = await client.DownloadCertificateAsync(certificateProperties.Name);
                caCerts.Add(certificateWithPolicy.Value);
            }
        }
    }
    else if (!string.IsNullOrEmpty(config.ServerSideCerts))
    {
        var pemCerts = File.ReadAllText(config.ServerSideCerts)
            .Split(new string[] { "-----END CERTIFICATE-----" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pem => pem + "\n-----END CERTIFICATE-----")
            .Select(pem => new X509Certificate2(Convert.FromBase64String(
            pem.Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("\n", "")
        )))
        .ToArray();

        caCerts.AddRange(pemCerts);
    }


    host.ConfigureFunctionsWebApplication();
    host.ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services
            .AddMeshClient(_ => _.MeshApiBaseUrl = config.MeshApiBaseUrl)
            .AddMailbox(config.BSSMailBox, new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = config.MeshPassword,
                SharedKey = config.MeshSharedKey,
                Cert = cert,
                serverSideCertCollection = caCerts
            })
            .Build();
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler, MeshToBlobTransferHandler>();
        // Register health checks
        services.AddBlobStorageHealthCheck("RetrieveMeshFile");
    })
    .AddExceptionHandler();

    var app = host.Build();

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to start up Function");
}


