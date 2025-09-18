using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HealthChecks.Extensions;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using DataServices.Core;

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .AddConfiguration<ManageCaasSubscriptionConfig>(out ManageCaasSubscriptionConfig? config)
    .AddMeshMailboxes(new MeshConfig
    {
        MeshApiBaseUrl = config!.CaasSubscriptionMeshApiBaseUrl,
        KeyVaultConnectionString = config.KeyVaultConnectionString,
        BypassServerCertificateValidation = config.BypassServerCertificateValidation,
        MailboxConfigs = new List<MailboxConfig>
        {
            new MailboxConfig
            {
                MailboxId = config.CaasFromMailbox,
                MeshKeyName = config.MeshCaasKeyName!,
                MeshKeyPassword = config.MeshCaasKeyPassword,
                MeshPassword = config.MeshCaasPassword,
                SharedKey = config.MeshCaasSharedKey
            }
        }
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddBasicHealthCheck("ManageCaasSubscription");
        services.AddScoped<IMeshSendCaasSubscribe, MeshSendCaasSubscribe>();
        services.AddScoped<IMeshPoller, MeshPoller>();
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddHttpClient()
    .AddTelemetry()
    .AddExceptionHandler();

// Log startup mode for visibility
var startupLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var startupLogger = startupLoggerFactory.CreateLogger("ManageCaasSubscription.Program");
startupLogger.LogInformation("ManageCaasSubscription starting with real MESH services. BaseUrl={Base}", config!.CaasSubscriptionMeshApiBaseUrl);

// Optionally seed WireMock success mapping for Mesh outbox when enabled
if (config.UseWireMock)
{
    await WireMockAdminHelper.SeedMeshSuccessMappingAsync(startupLogger, config.WireMockAdminUrl);
}

var host = hostBuilder.Build();
await host.RunAsync();
