using DataServices.Core;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using Model;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Program");

var host = new HostBuilder();

// Load configuration
host.AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config);

// Load NEMS certificate up-front and inject into DI
X509Certificate2? nemsCertificate = null;

if (!string.IsNullOrEmpty(config.KeyVaultConnectionString))
{
    logger.LogInformation("Loading NEMS certificate from Azure Key Vault");
    var certClient = new CertificateClient(
        new Uri(config.KeyVaultConnectionString),
        new DefaultAzureCredential()
    );
    var certResult = await certClient.DownloadCertificateAsync(config.NemsKeyName);
    nemsCertificate = certResult.Value;
}
else if (!string.IsNullOrEmpty(config.NemsLocalCertPath))
{
    logger.LogInformation("Loading NEMS certificate from local file");
    if (!string.IsNullOrEmpty(config.NemsLocalCertPassword))
        nemsCertificate = new X509Certificate2(config.NemsLocalCertPath, config.NemsLocalCertPassword);
    else
        nemsCertificate = new X509Certificate2(config.NemsLocalCertPath);
}
else
{
    throw new InvalidOperationException("No certificate configuration found. Please configure either KeyVaultConnectionString or NemsLocalCertPath.");
}

host.ConfigureFunctionsWebApplication();
host.ConfigureServices(services =>
{
    // Register HTTP client services
    services.AddHttpClient();
    services.AddScoped<IHttpClientFunction, HttpClientFunction>();

    // Register NEMS-specific services
    services.AddScoped<INemsHttpClientProvider, NemsHttpClientProvider>();
    services.AddScoped<INemsHttpClientFunction, NemsHttpClientFunction>();

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
        config.NemsFhirEndpoint,
        config.OdsCode,
        string.IsNullOrEmpty(config.MeshMailboxId) ? "NOT_SET" : "SET");

    logger.LogInformation("Config Debug -- NemsFhirEndpoint: {NemsFhirEndpoint}, FromAsid: {FromAsid}, LocalCert: {LocalCert}",
        config.NemsFhirEndpoint, config.FromAsid, config.NemsLocalCertPath);
})
.AddDataServicesHandler<DataServicesContext>()
.AddTelemetry()
.AddExceptionHandler();

var app = host.Build();
await app.RunAsync();
