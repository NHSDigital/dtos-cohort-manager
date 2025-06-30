using DataServices.Core;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using Model;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Program");

try
{
    var host = new HostBuilder();

    // Use your custom AddConfiguration method that works in other functions
    host.AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config);

    host.ConfigureFunctionsWebApplication();
    host.ConfigureServices(services =>
    {
        // Register HTTP client services
        services.AddHttpClient();
        services.AddScoped<IHttpClientFunction, HttpClientFunction>();

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

        logger.LogInformation("Config Debug -- NemsFhirEndpoint: {0}, FromAsid: {1}, LocalCert: {2}",
    config.NemsFhirEndpoint, config.FromAsid, config.NemsLocalCertPath);


        // Validate critical configuration
        ValidateConfiguration(config, logger);
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddTelemetry()
    .AddExceptionHandler();

    var app = host.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to start up NEMS Function");
    throw;
}

/// <summary>
/// Validates that all required configuration is present
/// </summary>
static void ValidateConfiguration(ManageNemsSubscriptionConfig config, ILogger logger)
{
    var errors = new List<string>();

    if (string.IsNullOrEmpty(config.NemsFhirEndpoint))
        errors.Add("NemsFhirEndpoint is required");

    if (string.IsNullOrEmpty(config.FromAsid))
        errors.Add("FromAsid is required");

    if (string.IsNullOrEmpty(config.ToAsid))
        errors.Add("ToAsid is required");

    if (string.IsNullOrEmpty(config.OdsCode))
        errors.Add("OdsCode is required");

    if (string.IsNullOrEmpty(config.MeshMailboxId))
        errors.Add("MeshMailboxId is required");

    // Certificate configuration - either KeyVault or local cert path
    if (string.IsNullOrEmpty(config.KeyVaultConnectionString) && string.IsNullOrEmpty(config.NemsLocalCertPath))
        errors.Add("Either KeyVaultConnectionString or NemsLocalCertPath must be configured");

    if (errors.Any())
    {
        var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"- {e}"));
        logger.LogCritical(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    logger.LogInformation("Configuration validation passed");
}
