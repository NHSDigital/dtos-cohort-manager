using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;

var host = new HostBuilder()
    .AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config)
    .AddDataServicesHandler()
    .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        // Register health checks
        services.AddBasicHealthCheck("NEMSSubscription");
    })
    .AddHttpClient()
    .Build();

await host.RunAsync();
