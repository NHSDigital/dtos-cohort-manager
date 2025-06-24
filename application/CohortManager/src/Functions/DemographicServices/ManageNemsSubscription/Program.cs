using DataServices.Core;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using NHS.CohortManager.DemographicServices;
using DataServices.Database;
using Azure.Data.Tables;

var host = new HostBuilder()
    .AddConfiguration<ManageNemsSubscriptionConfig>(out ManageNemsSubscriptionConfig config)
    .ConfigureFunctionsWebApplication()
    .AddDataServicesHandler<DataServicesContext>()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddScoped<NemsSubscriptionManager>();
        // Register health checks
        services.AddDatabaseHealthCheck("NEMSSubscription");
    })
    .AddTelemetry()
    .AddHttpClient()
    .Build();

await host.RunAsync();
