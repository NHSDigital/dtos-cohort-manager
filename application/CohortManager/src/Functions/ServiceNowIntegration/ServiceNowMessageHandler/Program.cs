using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService;

var host = new HostBuilder()
    .AddConfiguration(out ServiceNowMessageHandlerConfig config)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IServiceNowClient, ServiceNowClient>();
        services.AddMemoryCache();
        services.AddHttpClient();
    })
    .AddTelemetry()
    .AddServiceBusClient(config.ServiceBusConnectionString_client_internal)
    .Build();

await host.RunAsync();
