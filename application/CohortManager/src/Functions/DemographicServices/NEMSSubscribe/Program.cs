using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;
using Model;

var host = new HostBuilder()
    .AddConfiguration<NEMSSubscribeConfig>(out NEMSSubscribeConfig config)
    .AddDataServicesHandler()
    .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddHttpClient();
        services.AddScoped<IHttpClientFunction, HttpClientFunction>();
        // Register health checks
        services.AddBasicHealthCheck("NEMSSubscription");
    })
    .Build();

await host.RunAsync();
