using DataServices.Core;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Program");

var host = new HostBuilder();


logger.LogInformation("Application Has Started");
// Load configuration
host.AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config);

var nemsConfig = config;

X509Certificate2 nemsCertificate;


// Load NEMS certificate up-front and inject into DI

nemsCertificate = await nemsConfig.LoadNemsCertificateAsync(logger);


host.ConfigureFunctionsWebApplication();
host.AddHttpClient()
    .AddNemsHttpClient(nemsConfig.IsStubbed)
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
            nemsConfig.NemsOdsCode,
            string.IsNullOrEmpty(nemsConfig.NemsMeshMailboxId) ? "NOT_SET" : "SET");
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddTelemetry()
    .AddExceptionHandler();

var app = host.Build();
await app.RunAsync();
