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
    .AddConfiguration<MeshSendCaasSubscribeConfig>()
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
        if (config.IsStubbed)
        {
            services.AddSingleton<IMeshSendCaasSubscribe, MeshSendCaasSubscribeStub>();
            services.AddSingleton<IMeshPoller, MeshPollerStub>();
        }
        else
        {
            services.AddScoped<IMeshSendCaasSubscribe, MeshSendCaasSubscribe>();
            services.AddScoped<IMeshPoller, MeshPoller>();
        }
    })
    .AddDataServicesHandler<DataServicesContext>()
    .AddHttpClient()
    .AddTelemetry()
    .AddExceptionHandler();

// Log startup mode for visibility
var startupLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var startupLogger = startupLoggerFactory.CreateLogger("ManageCaasSubscription.Program");
if (config!.IsStubbed)
{
    startupLogger.LogWarning("ManageCaasSubscription starting in STUBBED mode: using MeshSendCaasSubscribeStub and MeshPollerStub.");
}
else
{
    startupLogger.LogInformation("ManageCaasSubscription starting in LIVE mode: using real MESH services.");
}

var host = hostBuilder.Build();
await host.RunAsync();
