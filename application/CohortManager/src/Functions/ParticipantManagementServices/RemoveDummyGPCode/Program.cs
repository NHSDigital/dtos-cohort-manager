using Common;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ParticipantManagementServices;

var host = new HostBuilder()
    .AddConfiguration(out RemoveDummyGpCodeConfig config)
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddBasicHealthCheck("RemoveDummyGPCode");
    })
    .AddTelemetry()
    .AddHttpClient()
    .AddServiceBusClient(config.ServiceBusConnectionString_client_internal)
    .Build();

await host.RunAsync();
