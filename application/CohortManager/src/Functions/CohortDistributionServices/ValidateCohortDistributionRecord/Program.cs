using Common;
using Data.Database;
using DataServices.Client;
using HealthChecks.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Model;

var host = new HostBuilder()
.AddDataServicesHandler()
        .AddDataService<CohortDistribution>(Environment.GetEnvironmentVariable("CohortDistributionDataServiceURL"))
        .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.TryAddTransient<IDatabaseHelper, DatabaseHelper>();
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<ICreateParticipant, CreateParticipant>();
        // Register health checks
        services.AddDatabaseHealthCheck("ValidateCohortDistributionRecord");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
