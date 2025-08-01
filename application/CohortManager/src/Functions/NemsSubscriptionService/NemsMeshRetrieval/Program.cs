using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client;
using Common;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using NHS.Screening.NemsMeshRetrieval;
using HealthChecks.Extensions;
using Azure.Security.KeyVault.Secrets;
using NHS.CohortManager.CaasIntegrationService;


var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("program.cs");

try
{
    var host = new HostBuilder();

    X509Certificate2 cohortManagerPrivateKey = null;
    X509Certificate2Collection meshCerts = [];

    host.AddConfiguration<NemsMeshRetrievalConfig>(out NemsMeshRetrievalConfig config);

    // Azure
    if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
    {
        // Get CohortManager private key
        logger.LogInformation("Pulling Mesh Certificate from KeyVault");
        var certClient = new CertificateClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
        var certificate = await certClient.DownloadCertificateAsync(config.NemsMeshKeyName);
        cohortManagerPrivateKey = certificate.Value;

        // Get MESH public certificates (CA chain)
        var secretClient = new SecretClient(vaultUri: new Uri(config.KeyVaultConnectionString), credential: new DefaultAzureCredential());
        string base64Cert = secretClient.GetSecret(config.NemsMeshCertName).Value.Value;
        meshCerts = CertificateHelper.GetCertificatesFromString(base64Cert);
    }
    // Local
    else
    {
        logger.LogInformation("Pulling Mesh Certificate from local File");
        cohortManagerPrivateKey = new X509Certificate2(config.NemsMeshKeyName, config.NemsMeshKeyPassphrase);

        string certsString = await File.ReadAllTextAsync(config.NemsMeshServerSideCerts);
        meshCerts = CertificateHelper.GetCertificatesFromString(certsString);
    }

    host.ConfigureFunctionsWebApplication();
    host.ConfigureServices(services =>
    {
        services
            .AddMeshClient(_ =>
            {
                _.MeshApiBaseUrl = config.NemsMeshApiBaseUrl;
                _.BypassServerCertificateValidation = config.NemsMeshBypassServerCertificateValidation ?? false;
            })
            .AddMailbox(config.NemsMeshMailBox, new NHS.MESH.Client.Configuration.MailboxConfiguration
            {
                Password = config.NemsMeshPassword,
                SharedKey = config.NemsMeshSharedKey,
                Cert = cohortManagerPrivateKey,
                serverSideCertCollection = meshCerts
            })
            .Build();
        services.AddSingleton<IBlobStorageHelper, BlobStorageHelper>();
        services.AddTransient<IMeshToBlobTransferHandler, MeshToBlobTransferHandler>();
        // Register health checks
        services.AddBlobStorageHealthCheck("NemsMeshRetrieval");
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


