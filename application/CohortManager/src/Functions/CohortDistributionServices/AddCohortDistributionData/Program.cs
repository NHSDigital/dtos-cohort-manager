using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model;
using HealthChecks.Extensions;
using NHS.Screening.AddCohortDistribution;

var host = new HostBuilder()
    .AddConfiguration<AddCohortDistributionDataConfig>(out AddCohortDistributionDataConfig config)
    .AddDataServicesHandler()
    .AddDataService<CohortDistribution>(config.CohortDistributionDataServiceURL)
    .Build()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICreateResponse, CreateResponse>();
        services.AddSingleton<IDatabaseHelper, DatabaseHelper>();
        // Register health checks
        services.AddBasicHealthCheck("AddCohortDistributionData");
    })
    .AddDatabaseConnection()
    .AddExceptionHandler()
    .Build();

await host.RunAsync();
