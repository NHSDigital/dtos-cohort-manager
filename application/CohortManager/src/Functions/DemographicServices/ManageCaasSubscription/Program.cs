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
        MeshApiBaseUrl = config!.MeshApiBaseUrl,
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

var host = hostBuilder.Build();
await host.RunAsync();
