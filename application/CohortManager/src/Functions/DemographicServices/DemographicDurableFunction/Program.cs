using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using Model;
using DataServices.Client;
using NHS.Screening.DemographicDurableFunction;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddConfiguration<DemographicDurableFunctionConfig>(out DemographicDurableFunctionConfig config)
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(config.DemographicDataServiceURL)
        .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        // Register health checks
        services.AddDatabaseHealthCheck("DurableDemographicFunction");
    })
    .AddTelemetry()
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .AddHttpClient()
    .Build();

await host.RunAsync();
