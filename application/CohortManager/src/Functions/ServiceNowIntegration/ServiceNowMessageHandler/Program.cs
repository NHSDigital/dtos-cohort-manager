using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService;

var host = new HostBuilder()
    .AddConfiguration<ServiceNowMessageHandlerConfig>()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IServiceNowClient, ServiceNowClient>();
        services.AddMemoryCache();
        services.AddHttpClient();
    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
