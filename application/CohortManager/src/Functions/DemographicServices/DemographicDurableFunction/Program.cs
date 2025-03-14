using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data.Database;
using Model;
using DataServices.Client;
using HealthChecks.Extensions;

var host = new HostBuilder()
    .AddDataServicesHandler()
        .AddDataService<ParticipantDemographic>(Environment.GetEnvironmentVariable("DemographicDataServiceURL"))
        .Build()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddTransient<IDatabaseHelper, DatabaseHelper>();
        // Register health checks
        services.AddDatabaseHealthCheck("DurableDemographicFunction");
    })
    .AddExceptionHandler()
    .AddDatabaseConnection()
    .Build();

await host.RunAsync();
