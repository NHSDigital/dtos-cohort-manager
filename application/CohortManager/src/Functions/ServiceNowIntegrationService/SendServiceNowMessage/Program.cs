using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService;



var host = new HostBuilder()
    .AddConfiguration<SendServiceNowMsgConfig>(out SendServiceNowMsgConfig config)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient<ServiceNowMessageHandler>();

    })
    .Build();

await host.RunAsync();
