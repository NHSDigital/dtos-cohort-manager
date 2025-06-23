using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService;



var host = new HostBuilder()
    .AddConfiguration<SendServiceNowMsgConfig>()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient<ServiceNowMessageHandler>();

    })
    .AddTelemetry()
    .Build();

await host.RunAsync();
