using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using Common;
using NHS.Screening.NEMSSubscribe;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient();
        // Register health checks
        services.AddBasicHealthCheck("NEMSSubscription");
    })
    .Build();

await host.RunAsync();
