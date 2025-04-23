using Common;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
