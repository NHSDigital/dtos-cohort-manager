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
using Azure.Security.KeyVault.Secrets;


var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");

try
{
    var host = new HostBuilder();

    X509Certificate2 cohortManagerPrivateKey = null!;
    X509Certificate2Collection meshCerts = [];

    host.AddConfiguration<RetrieveMeshFileConfig>(out RetrieveMeshFileConfig config);

    // Azure
    if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
    {
        // Get CohortManager private key
        logger.LogInformation("Pulling Mesh Certificate from KeyVault");
        var certClient = new CertificateClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
        var certificate = await certClient.DownloadCertificateAsync(config.MeshKeyName);
        cohortManagerPrivateKey = certificate.Value;

        // Get MESH public certificates (CA chain)
        var secretClient = new SecretClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
        string base64Cert = (await secretClient.GetSecretAsync(config.MeshCertName)).Value.Value;
        meshCerts = CertificateHelper.GetCertificatesFromString(base64Cert);
    }
    // Local
    else
    {
        logger.LogInformation("Pulling Mesh Certificate from local File");
        cohortManagerPrivateKey = new X509Certificate2(config.MeshKeyName!, config.MeshKeyPassphrase);

        string certsString = await File.ReadAllTextAsync(config.ServerSideCerts!);
        meshCerts = CertificateHelper.GetCertificatesFromString(certsString);
    }

    host.ConfigureFunctionsWebApplication();
    host.ConfigureServices(services =>
    {
        services
            .AddMeshClient(_ =>
            {
                _.MeshApiBaseUrl = config.MeshApiBaseUrl;
                _.BypassServerCertificateValidation = config.BypassServerCertificateValidation ?? false;
            })
            .AddMailbox(config.BSSMailBox, new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = config.MeshPassword,
                SharedKey = config.MeshSharedKey,
                Cert = cohortManagerPrivateKey,
                serverSideCertCollection = meshCerts
            })
            .Build();
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler, MeshToBlobTransferHandler>();
        // Register health checks
        services.AddBlobStorageHealthCheck("RetrieveMeshFile");
    })
    .AddTelemetry()
    .AddExceptionHandler();

    var app = host.Build();

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to start up Function");
}


