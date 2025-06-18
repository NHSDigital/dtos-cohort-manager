using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowMessageHandler;


var host = new HostBuilder()
    .AddConfiguration<SendServiceNowMsgConfig>()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient<ServiceNowMsgHandler>();

    })
    .Build();

await host.RunAsync();
