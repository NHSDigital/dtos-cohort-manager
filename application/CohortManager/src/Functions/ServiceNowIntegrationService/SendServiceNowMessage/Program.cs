using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHS.CohortManager.ServiceNowIntegrationService.ServiceNowMessageService;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient<SendServiceNowMessageFunction>();
        services.Configure<SendServiceNowMsgConfig>(context.Configuration);

    })
    .Build();

await host.RunAsync();
