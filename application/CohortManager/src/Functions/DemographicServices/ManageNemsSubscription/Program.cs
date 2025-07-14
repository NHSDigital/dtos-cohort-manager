using DataServices.Core;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices.ManageNemsSubscription.Config;
using NHS.CohortManager.DemographicServices.ManageNemsSubscription;
using DataServices.Database;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Program");

var host = new HostBuilder();

// Load configuration
host.AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config);

var nemsConfig = config.ManageNemsSubscription;

// Load NEMS certificate up-front and inject into DI
X509Certificate2? nemsCertificate = null;

logger.LogInformation(nemsConfig.NemsLocalCertPath);

if (!string.IsNullOrEmpty(nemsConfig.KeyVaultConnectionString))
{
    logger.LogInformation("Loading NEMS certificate from Azure Key Vault");
    var certClient = new CertificateClient(
        new Uri(nemsConfig.KeyVaultConnectionString),
        new ManagedIdentityCredential()
    );
    var certResult = await certClient.DownloadCertificateAsync(nemsConfig.NemsKeyName);
    nemsCertificate = certResult.Value;
}
else if (!string.IsNullOrEmpty(nemsConfig.NemsLocalCertPath))
{
    logger.LogInformation("Loading NEMS certificate from local file");
    if (!string.IsNullOrEmpty(nemsConfig.NemsLocalCertPassword))
        nemsCertificate = new X509Certificate2(nemsConfig.NemsLocalCertPath, nemsConfig.NemsLocalCertPassword);
    else
        nemsCertificate = new X509Certificate2(nemsConfig.NemsLocalCertPath);
}
else
{
    throw new InvalidOperationException("No certificate configuration found. Please configure either KeyVaultConnectionString or NemsLocalCertPath.");
}

host.ConfigureFunctionsWebApplication();
host.AddHttpClient()
    .AddNemsHttpClient()
    .ConfigureServices(services =>
    {
        // Register NEMS certificate
        services.AddSingleton(nemsCertificate);

        // Register NEMS subscription manager
        services.AddScoped<NemsSubscriptionManager>();

        // Register response helpers
        services.AddSingleton<ICreateResponse, CreateResponse>();

        // Register health checks
        services.AddDatabaseHealthCheck("NEMSSubscription");

        // Log configuration for debugging (without sensitive data)
        logger.LogInformation("NEMS Configuration loaded - Endpoint: {Endpoint}, ODS: {OdsCode}, MESH: {MeshId}",
            nemsConfig.NemsFhirEndpoint,
            nemsConfig.OdsCode,
            string.IsNullOrEmpty(nemsConfig.MeshMailboxId) ? "NOT_SET" : "SET");
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddTelemetry()
    .AddExceptionHandler();

var app = host.Build();
await app.RunAsync();
